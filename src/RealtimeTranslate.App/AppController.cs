using System.Diagnostics;
using System.IO;
using RealtimeTranslate.Core.Capture;
using RealtimeTranslate.Core.Dictionary;
using RealtimeTranslate.Core.Models;
using RealtimeTranslate.Core.Normalization;
using RealtimeTranslate.Core.Ocr;
using RealtimeTranslate.Core.Presentation;
using RealtimeTranslate.Core.Recording;
using RealtimeTranslate.Core.Selection;
using RealtimeTranslate.Core.Stats;
using RealtimeTranslate.App.Settings;
using RealtimeTranslate.App.SystemIntegration;
using AppSelectionMode = RealtimeTranslate.App.Settings.SelectionMode;

namespace RealtimeTranslate.App;

public sealed class AppController : IDisposable
{
    private readonly AppSettings _settings;
    private readonly ISelectionService _selectionService;
    private readonly IScreenshotService _screenshotService;
    private readonly IOcrService _ocrService;
    private readonly IWordNormalizer _wordNormalizer;
    private readonly IDictionaryService _dictionaryService;
    private readonly IOverlayPresenter _overlayPresenter;
    private readonly ILookupRecorder _lookupRecorder;
    private readonly IWordQueryCounter _queryCounter;
    private readonly UserPreferencesStore _preferencesStore;
    private readonly GlobalHotkeyService _hotkeyService;
    private readonly GlobalRightDragSelectionService _rightDragSelectionService;
    private readonly TrayIconService _trayIconService;
    private UserPreferences _preferences = UserPreferences.Default;
    private CancellationTokenSource? _selectionCancellation;
    private bool _isSelecting;
    private bool _continuousModeActive;

    public AppController(
        AppSettings settings,
        ISelectionService selectionService,
        IScreenshotService screenshotService,
        IOcrService ocrService,
        IWordNormalizer wordNormalizer,
        IDictionaryService dictionaryService,
        IOverlayPresenter overlayPresenter,
        ILookupRecorder lookupRecorder,
        IWordQueryCounter queryCounter,
        UserPreferencesStore preferencesStore)
    {
        _settings = settings;
        _selectionService = selectionService;
        _screenshotService = screenshotService;
        _ocrService = ocrService;
        _wordNormalizer = wordNormalizer;
        _dictionaryService = dictionaryService;
        _overlayPresenter = overlayPresenter;
        _lookupRecorder = lookupRecorder;
        _queryCounter = queryCounter;
        _preferencesStore = preferencesStore;
        _hotkeyService = new GlobalHotkeyService();
        _rightDragSelectionService = new GlobalRightDragSelectionService();
        _trayIconService = new TrayIconService(TriggerSelectionCommand, ConfigureHotkey, SetSelectionMode, OpenArchiveFolder, Exit);
        _hotkeyService.Pressed += (_, _) => TriggerSelectionCommand();
        _rightDragSelectionService.SelectionCompleted += (_, selection) => StartLookup(selection);
    }

    public void Start()
    {
        Directory.CreateDirectory(_settings.ArchiveRoot);
        Directory.CreateDirectory(Path.Combine(_settings.ArchiveRoot, "screenshots"));
        Directory.CreateDirectory(Path.Combine(_settings.ArchiveRoot, "records"));
        _preferences = _preferencesStore.Load();
        RegisterHotkey(_preferences.Hotkey);
        _rightDragSelectionService.Start();
        UpdateTray();
        _trayIconService.ShowInfo("Realtime Translate Codex", $"Ready. Right-drag a word, or press {_preferences.Hotkey}.");
    }

    public void Dispose()
    {
        _rightDragSelectionService.Dispose();
        _hotkeyService.Dispose();
        _trayIconService.Dispose();
    }

    private void TriggerSelectionCommand()
    {
        if (_preferences.SelectionMode == AppSelectionMode.Continuous)
        {
            ToggleContinuousSelection();
            return;
        }

        StartSelection();
    }

    private void StartSelection()
    {
        if (_isSelecting)
        {
            return;
        }

        _ = SelectAndLookupAsync();
    }

    private void ToggleContinuousSelection()
    {
        if (_continuousModeActive)
        {
            StopContinuousSelection();
            return;
        }

        if (_isSelecting)
        {
            return;
        }

        _continuousModeActive = true;
        UpdateTray();
        _ = ContinuousSelectionLoopAsync();
    }

    private void StopContinuousSelection()
    {
        _continuousModeActive = false;
        _selectionCancellation?.Cancel();
        UpdateTray();
    }

    private void StartLookup(ScreenSelection selection)
    {
        if (_isSelecting)
        {
            return;
        }

        _ = LookupSelectionAsync(selection);
    }

    private async Task SelectAndLookupAsync()
    {
        _isSelecting = true;
        _selectionCancellation = new CancellationTokenSource();
        try
        {
            var selection = await _selectionService.SelectAsync(_selectionCancellation.Token);
            if (selection is null)
            {
                return;
            }

            await LookupSelectionCoreAsync(selection);
        }
        finally
        {
            _selectionCancellation.Dispose();
            _selectionCancellation = null;
            _isSelecting = false;
        }
    }

    private async Task ContinuousSelectionLoopAsync()
    {
        try
        {
            while (_continuousModeActive)
            {
                _isSelecting = true;
                _selectionCancellation = new CancellationTokenSource();
                try
                {
                    var selection = await _selectionService.SelectAsync(_selectionCancellation.Token);
                    if (selection is null)
                    {
                        _continuousModeActive = false;
                        break;
                    }

                    await LookupSelectionCoreAsync(selection);
                }
                finally
                {
                    _selectionCancellation.Dispose();
                    _selectionCancellation = null;
                    _isSelecting = false;
                }

                await Task.Delay(80);
            }
        }
        finally
        {
            _continuousModeActive = false;
            UpdateTray();
        }
    }

    private async Task LookupSelectionAsync(ScreenSelection selection)
    {
        _isSelecting = true;
        try
        {
            await LookupSelectionCoreAsync(selection);
        }
        finally
        {
            _isSelecting = false;
        }
    }

    private async Task LookupSelectionCoreAsync(ScreenSelection selection)
    {
        CapturedSelection? capture = null;
        IReadOnlyList<OcrCandidate> candidates = [];

        try
        {
            capture = await _screenshotService.CaptureAsync(selection);
            candidates = await _ocrService.RecognizeAsync(capture.CroppedImagePng);
            var recognizedText = candidates
                .OrderByDescending(candidate => candidate.Confidence)
                .ThenByDescending(candidate => candidate.Text.Length)
                .FirstOrDefault()
                ?.Text ?? string.Empty;

            if (string.IsNullOrWhiteSpace(recognizedText))
            {
                var record = BuildRecord(selection, capture.ScreenshotPath, string.Empty, NormalizedWord.Empty, ["未识别到单词"], candidates);
                SafeRecord(record);
                _overlayPresenter.Show(new LookupPresentation("未识别到单词", "OCR returned no word", ["请试着框得更贴近单词"], "请试着框得更贴近单词", 0, IsError: true));
                return;
            }

            var normalized = _wordNormalizer.Normalize(recognizedText);
            var lookup = _dictionaryService.Lookup(normalized);
            var definitions = lookup.Found ? lookup.Definitions : ["未找到释义"];
            var title = string.IsNullOrWhiteSpace(normalized.DisplayWord) ? recognizedText : normalized.DisplayWord;
            var countKey = string.IsNullOrWhiteSpace(normalized.Lemma) ? title : normalized.Lemma;
            var queryCount = _queryCounter.Increment(countKey);
            var primaryDefinition = definitions.FirstOrDefault() ?? string.Empty;
            var subtitle = lookup.Found
                ? $"lemma: {lookup.EntryWord} · OCR: {recognizedText}"
                : $"lemma: {normalized.Lemma} · OCR: {recognizedText}";

            SafeRecord(BuildRecord(selection, capture.ScreenshotPath, recognizedText, normalized, definitions, candidates));
            _overlayPresenter.Show(new LookupPresentation(title, subtitle, definitions, primaryDefinition, queryCount, IsError: !lookup.Found));
        }
        catch (Exception ex)
        {
            var message = ex.Message.Length > 120 ? ex.Message[..120] : ex.Message;
            _overlayPresenter.Show(new LookupPresentation("查词失败", message, ["失败已被拦截，应用仍在运行"], "失败已被拦截", 0, IsError: true));

            if (selection is not null)
            {
                SafeRecord(new LookupRecord(
                    DateTimeOffset.Now,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    [$"查词失败: {message}"],
                    selection,
                    capture?.ScreenshotPath ?? string.Empty,
                candidates));
            }
        }
    }

    private static LookupRecord BuildRecord(
        ScreenSelection selection,
        string screenshotPath,
        string recognizedText,
        NormalizedWord normalized,
        IReadOnlyList<string> definitions,
        IReadOnlyList<OcrCandidate> candidates)
    {
        return new LookupRecord(
            DateTimeOffset.Now,
            recognizedText,
            normalized.DisplayWord,
            normalized.Lemma,
            definitions,
            selection,
            screenshotPath,
            candidates);
    }

    private void SafeRecord(LookupRecord record)
    {
        try
        {
            _lookupRecorder.Record(record);
        }
        catch (Exception ex)
        {
            _trayIconService.ShowInfo("Archive write failed", ex.Message);
        }
    }

    private void OpenArchiveFolder()
    {
        Directory.CreateDirectory(_settings.ArchiveRoot);
        Process.Start(new ProcessStartInfo(_settings.ArchiveRoot)
        {
            UseShellExecute = true,
        });
    }

    private void Exit()
    {
        System.Windows.Application.Current.Shutdown();
    }

    private void ConfigureHotkey()
    {
        var window = new HotkeySettingsWindow(_preferences.Hotkey);
        if (window.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var updated = _preferences with { Hotkey = window.CapturedHotkey };
            RegisterHotkey(updated.Hotkey);
            SavePreferences(updated);
            _trayIconService.ShowInfo("Hotkey updated", $"Current hotkey: {updated.Hotkey}");
        }
        catch (Exception ex)
        {
            try
            {
                RegisterHotkey(_preferences.Hotkey);
            }
            catch
            {
                // The tray menu remains available even if restoring the old global hotkey fails.
            }

            _trayIconService.ShowInfo("Hotkey update failed", ex.Message);
        }
    }

    private void SetSelectionMode(AppSelectionMode selectionMode)
    {
        if (_preferences.SelectionMode == selectionMode)
        {
            return;
        }

        if (_continuousModeActive && selectionMode == AppSelectionMode.OneShot)
        {
            StopContinuousSelection();
        }

        SavePreferences(_preferences with { SelectionMode = selectionMode });
    }

    private void RegisterHotkey(HotkeyGesture hotkey)
    {
        _hotkeyService.Register(hotkey);
    }

    private void SavePreferences(UserPreferences preferences)
    {
        _preferences = preferences;
        _preferencesStore.Save(preferences);
        UpdateTray();
    }

    private void UpdateTray()
    {
        _trayIconService.UpdatePreferences(_preferences, _continuousModeActive);
    }
}
