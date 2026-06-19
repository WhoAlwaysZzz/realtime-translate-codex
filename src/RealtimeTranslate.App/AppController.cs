using System.Diagnostics;
using System.IO;
using RealtimeTranslate.App.ManualInput;
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
using RealtimeTranslate.Core.Explanation;
using RealtimeTranslate.Core.History;

namespace RealtimeTranslate.App;

public sealed class AppController : IDisposable
{
    private readonly AppSettings _settings;
    private readonly ISelectionService _selectionService;
    private readonly IScreenshotService _screenshotService;
    private readonly IOcrService _ocrService;
    private readonly OcrTextComposer _ocrTextComposer;
    private readonly IWordNormalizer _wordNormalizer;
    private readonly IDictionaryService _dictionaryService;
    private readonly IOverlayPresenter _overlayPresenter;
    private readonly ITextExplanationPresenter _textExplanationPresenter;
    private readonly ITextExplanationService _textExplanationService;
    private readonly IRecentLookupStore _recentLookupStore;
    private readonly ILookupRecorder _lookupRecorder;
    private readonly ITextExplanationRecorder _textExplanationRecorder;
    private readonly IWordQueryCounter _queryCounter;
    private readonly UserPreferencesStore _preferencesStore;
    private readonly Dictionary<LookupCommand, GlobalHotkeyService> _hotkeyServices;
    private readonly GlobalRightDragSelectionService _rightDragSelectionService;
    private readonly TrayIconService _trayIconService;
    private UserPreferences _preferences = UserPreferences.Default;
    private CancellationTokenSource? _selectionCancellation;
    private bool _selectionInProgress;
    private bool _continuousModeActive;
    private ManualLookupWindow? _manualLookupWindow;

    public AppController(
        AppSettings settings,
        ISelectionService selectionService,
        IScreenshotService screenshotService,
        IOcrService ocrService,
        OcrTextComposer ocrTextComposer,
        IWordNormalizer wordNormalizer,
        IDictionaryService dictionaryService,
        IOverlayPresenter overlayPresenter,
        ITextExplanationPresenter textExplanationPresenter,
        ITextExplanationService textExplanationService,
        IRecentLookupStore recentLookupStore,
        ILookupRecorder lookupRecorder,
        ITextExplanationRecorder textExplanationRecorder,
        IWordQueryCounter queryCounter,
        UserPreferencesStore preferencesStore)
    {
        _settings = settings;
        _selectionService = selectionService;
        _screenshotService = screenshotService;
        _ocrService = ocrService;
        _ocrTextComposer = ocrTextComposer;
        _wordNormalizer = wordNormalizer;
        _dictionaryService = dictionaryService;
        _overlayPresenter = overlayPresenter;
        _textExplanationPresenter = textExplanationPresenter;
        _textExplanationService = textExplanationService;
        _recentLookupStore = recentLookupStore;
        _lookupRecorder = lookupRecorder;
        _textExplanationRecorder = textExplanationRecorder;
        _queryCounter = queryCounter;
        _preferencesStore = preferencesStore;
        _hotkeyServices = new Dictionary<LookupCommand, GlobalHotkeyService>
        {
            [LookupCommand.Selection] = new(0x1999, "RealtimeTranslateCodexSelectionHotkey"),
            [LookupCommand.ManualLookup] = new(0x199A, "RealtimeTranslateCodexManualLookupHotkey"),
            [LookupCommand.RecentLookups] = new(0x199B, "RealtimeTranslateCodexRecentLookupsHotkey"),
            [LookupCommand.ExplainSelection] = new(0x199C, "RealtimeTranslateCodexExplainSelectionHotkey"),
        };
        _rightDragSelectionService = new GlobalRightDragSelectionService();
        _trayIconService = new TrayIconService(
            TriggerSelectionCommand,
            ShowManualLookupWindow,
            ShowRecentLookups,
            StartTextExplanationSelection,
            ConfigureHotkey,
            ConfigureManualLookupHotkey,
            ConfigureRecentLookupsHotkey,
            ConfigureExplainSelectionHotkey,
            SetSelectionMode,
            OpenArchiveFolder,
            Exit);
        _hotkeyServices[LookupCommand.Selection].Pressed += (_, _) => TriggerSelectionCommand();
        _hotkeyServices[LookupCommand.ManualLookup].Pressed += (_, _) => ShowManualLookupWindow();
        _hotkeyServices[LookupCommand.RecentLookups].Pressed += (_, _) => ShowRecentLookups();
        _hotkeyServices[LookupCommand.ExplainSelection].Pressed += (_, _) => StartTextExplanationSelection();
        _rightDragSelectionService.SelectionCompleted += (_, selection) => StartLookup(selection);
    }

    public void Start()
    {
        Directory.CreateDirectory(_settings.ArchiveRoot);
        Directory.CreateDirectory(Path.Combine(_settings.ArchiveRoot, "screenshots"));
        Directory.CreateDirectory(Path.Combine(_settings.ArchiveRoot, "records"));
        _preferences = _preferencesStore.Load();
        RegisterCommandHotkeys();
        _rightDragSelectionService.Start();
        UpdateTray();
        _trayIconService.ShowInfo(
            "Realtime Translate Codex",
            $"Ready. Right-drag a word, press {_preferences.Hotkey}, press {_preferences.ManualLookupHotkey} to type, {_preferences.RecentLookupsHotkey} to replay, or {_preferences.ExplainSelectionHotkey} to explain text.");
    }

    public void Dispose()
    {
        _rightDragSelectionService.Dispose();
        foreach (var service in _hotkeyServices.Values)
        {
            service.Dispose();
        }

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
        if (_selectionInProgress)
        {
            return;
        }

        _ = SelectAndLookupAsync();
    }

    private void StartTextExplanationSelection()
    {
        if (_selectionInProgress)
        {
            return;
        }

        _ = SelectAndExplainTextAsync();
    }

    private void ToggleContinuousSelection()
    {
        if (_continuousModeActive)
        {
            StopContinuousSelection();
            return;
        }

        if (_selectionInProgress)
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
        if (_selectionInProgress)
        {
            return;
        }

        _ = LookupSelectionAsync(selection);
    }

    private async Task SelectAndLookupAsync()
    {
        ScreenSelection? selection = null;

        try
        {
            _selectionInProgress = true;
            _selectionCancellation = new CancellationTokenSource();
            selection = await _selectionService.SelectAsync(_selectionCancellation.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        finally
        {
            _selectionCancellation?.Dispose();
            _selectionCancellation = null;
            _selectionInProgress = false;
            EnsureHotkeyRegistered();
        }

        if (selection is not null)
        {
            _ = LookupSelectionCoreAsync(selection);
        }
    }

    private async Task ContinuousSelectionLoopAsync()
    {
        try
        {
            while (_continuousModeActive)
            {
                ScreenSelection? selection = null;

                try
                {
                    _selectionInProgress = true;
                    _selectionCancellation = new CancellationTokenSource();
                    selection = await _selectionService.SelectAsync(_selectionCancellation.Token);
                }
                catch (OperationCanceledException)
                {
                    _continuousModeActive = false;
                    break;
                }
                finally
                {
                    _selectionCancellation?.Dispose();
                    _selectionCancellation = null;
                    _selectionInProgress = false;
                    EnsureHotkeyRegistered();
                }

                if (selection is null)
                {
                    _continuousModeActive = false;
                    break;
                }

                _ = LookupSelectionCoreAsync(selection);
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
        await LookupSelectionCoreAsync(selection);
    }

    private async Task SelectAndExplainTextAsync()
    {
        ScreenSelection? selection = null;

        try
        {
            _selectionInProgress = true;
            _selectionCancellation = new CancellationTokenSource();
            selection = await _selectionService.SelectAsync(_selectionCancellation.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        finally
        {
            _selectionCancellation?.Dispose();
            _selectionCancellation = null;
            _selectionInProgress = false;
            EnsureHotkeyRegistered();
        }

        if (selection is not null)
        {
            _ = ExplainSelectionCoreAsync(selection);
        }
    }

    private async Task ExplainSelectionCoreAsync(ScreenSelection selection)
    {
        CapturedSelection? capture = null;
        var sourceText = string.Empty;

        try
        {
            capture = await _screenshotService.CaptureAsync(selection);
            var candidates = await _ocrService.RecognizeAsync(capture.CroppedImagePng);
            sourceText = _ocrTextComposer.Compose(candidates);
            if (string.IsNullOrWhiteSpace(sourceText))
            {
                var message = "OCR did not recognize text for explanation.";
                ShowExplanationError("未识别到可解释文本", string.Empty, "请框选完整句子或短段落。");
                SafeRecordTextExplanation(BuildTextExplanationRecord(selection, capture.ScreenshotPath, string.Empty, string.Empty, [], string.Empty, string.Empty, message));
                return;
            }

            _trayIconService.ShowInfo("Explaining selected text", "Waiting for AI response...");
            var explanation = await _textExplanationService.ExplainAsync(sourceText);
            _textExplanationPresenter.Show(new TextExplanationPresentation(
                "整句解释",
                explanation.SourceText,
                explanation.Translation,
                explanation.Notes,
                $"{explanation.Provider} · {explanation.Model}"));
            SafeRecordTextExplanation(BuildTextExplanationRecord(
                selection,
                capture.ScreenshotPath,
                explanation.SourceText,
                explanation.Translation,
                explanation.Notes,
                explanation.Provider,
                explanation.Model,
                null));
        }
        catch (Exception ex)
        {
            var message = ex.Message.Length > 240 ? ex.Message[..240] : ex.Message;
            ShowExplanationError("解释失败", sourceText, message);
            SafeRecordTextExplanation(BuildTextExplanationRecord(
                selection,
                capture?.ScreenshotPath ?? string.Empty,
                sourceText,
                string.Empty,
                [],
                string.Empty,
                string.Empty,
                message));
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
            var presentation = new LookupPresentation(title, subtitle, definitions, primaryDefinition, queryCount, IsError: !lookup.Found);

            SafeRecord(BuildRecord(selection, capture.ScreenshotPath, recognizedText, normalized, definitions, candidates));
            ShowAndRemember(presentation);
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

    private Task LookupManualTextAsync(string text)
    {
        var normalized = _wordNormalizer.Normalize(text);
        if (string.IsNullOrWhiteSpace(normalized.DisplayWord))
        {
            _overlayPresenter.Show(new LookupPresentation("未识别到单词", "typed input", ["请输入英文单词"], "请输入英文单词", 0, IsError: true));
            return Task.CompletedTask;
        }

        ShowDictionaryResult(normalized, $"typed: {text.Trim()}");
        return Task.CompletedTask;
    }

    private void ShowDictionaryResult(NormalizedWord normalized, string source)
    {
        var lookup = _dictionaryService.Lookup(normalized);
        var definitions = lookup.Found ? lookup.Definitions : ["未找到释义"];
        var title = normalized.DisplayWord;
        var countKey = string.IsNullOrWhiteSpace(normalized.Lemma) ? title : normalized.Lemma;
        var queryCount = _queryCounter.Increment(countKey);
        var primaryDefinition = definitions.FirstOrDefault() ?? string.Empty;
        var subtitle = lookup.Found
            ? $"lemma: {lookup.EntryWord} · {source}"
            : $"lemma: {normalized.Lemma} · {source}";

        ShowAndRemember(new LookupPresentation(title, subtitle, definitions, primaryDefinition, queryCount, IsError: !lookup.Found));
    }

    private void ShowAndRemember(LookupPresentation presentation)
    {
        SafeRemember(presentation);
        _overlayPresenter.Show(presentation);
    }

    private void SafeRemember(LookupPresentation presentation)
    {
        try
        {
            _recentLookupStore.Add(presentation);
        }
        catch (Exception ex)
        {
            _trayIconService.ShowInfo("Recent lookup save failed", ex.Message);
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

    private static TextExplanationRecord BuildTextExplanationRecord(
        ScreenSelection selection,
        string screenshotPath,
        string sourceText,
        string translation,
        IReadOnlyList<string> notes,
        string provider,
        string model,
        string? errorMessage)
    {
        return new TextExplanationRecord(
            DateTimeOffset.Now,
            sourceText,
            translation,
            notes,
            selection,
            screenshotPath,
            provider,
            model,
            errorMessage);
    }

    private void SafeRecordTextExplanation(TextExplanationRecord record)
    {
        try
        {
            _textExplanationRecorder.Record(record);
        }
        catch (Exception ex)
        {
            _trayIconService.ShowInfo("Explanation archive write failed", ex.Message);
        }
    }

    private void ShowExplanationError(string title, string sourceText, string message)
    {
        _textExplanationPresenter.Show(new TextExplanationPresentation(
            title,
            sourceText,
            message,
            [],
            "AI explanation",
            IsError: true));
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

    private void ShowManualLookupWindow()
    {
        if (_manualLookupWindow is not null && _manualLookupWindow.IsVisible)
        {
            _manualLookupWindow.Activate();
            return;
        }

        _manualLookupWindow = new ManualLookupWindow(CompleteManualLookupPrefix);
        _manualLookupWindow.Closed += (_, _) => _manualLookupWindow = null;
        _manualLookupWindow.Submitted += (_, text) => _ = LookupManualTextAsync(text);
        _manualLookupWindow.Show();
    }

    private string? CompleteManualLookupPrefix(string prefix)
    {
        try
        {
            return _recentLookupStore.CompletePrefix(prefix);
        }
        catch (Exception ex)
        {
            _trayIconService.ShowInfo("Recent lookup completion failed", ex.Message);
            return null;
        }
    }

    private void ShowRecentLookups()
    {
        IReadOnlyList<LookupPresentation> recent;
        try
        {
            recent = _recentLookupStore.GetRecent(3);
        }
        catch (Exception ex)
        {
            _trayIconService.ShowInfo("Recent lookup replay failed", ex.Message);
            return;
        }

        if (recent.Count == 0)
        {
            _trayIconService.ShowInfo("No recent lookups", "Look up a word first.");
            return;
        }

        foreach (var presentation in recent)
        {
            _overlayPresenter.Show(presentation);
        }
    }

    private void ConfigureHotkey()
    {
        ConfigureCommandHotkey(LookupCommand.Selection);
    }

    private void ConfigureManualLookupHotkey()
    {
        ConfigureCommandHotkey(LookupCommand.ManualLookup);
    }

    private void ConfigureRecentLookupsHotkey()
    {
        ConfigureCommandHotkey(LookupCommand.RecentLookups);
    }

    private void ConfigureExplainSelectionHotkey()
    {
        ConfigureCommandHotkey(LookupCommand.ExplainSelection);
    }

    private void ConfigureCommandHotkey(LookupCommand command)
    {
        var window = new HotkeySettingsWindow(_preferences.GetHotkey(command));
        if (window.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var updated = _preferences.WithHotkey(command, window.CapturedHotkey);
            RegisterCommandHotkey(command, updated.GetHotkey(command));
            SavePreferences(updated);
            _trayIconService.ShowInfo($"{CommandDisplayName(command)} hotkey updated", $"Current hotkey: {updated.GetHotkey(command)}");
        }
        catch (Exception ex)
        {
            try
            {
                RegisterCommandHotkey(command, _preferences.GetHotkey(command));
            }
            catch
            {
                // The tray menu remains available even if restoring the old global hotkey fails.
            }

            _trayIconService.ShowInfo($"{CommandDisplayName(command)} hotkey update failed", ex.Message);
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

    private void RegisterCommandHotkeys()
    {
        foreach (var command in Enum.GetValues<LookupCommand>())
        {
            RegisterCommandHotkey(command, _preferences.GetHotkey(command));
        }
    }

    private void RegisterCommandHotkey(LookupCommand command, HotkeyGesture hotkey)
    {
        _hotkeyServices[command].Register(hotkey);
    }

    private void EnsureHotkeyRegistered()
    {
        foreach (var command in Enum.GetValues<LookupCommand>())
        {
            var service = _hotkeyServices[command];
            var preferredHotkey = _preferences.GetHotkey(command);
            if (service.RegisteredHotkey == preferredHotkey)
            {
                continue;
            }

            try
            {
                RegisterCommandHotkey(command, preferredHotkey);
            }
            catch (Exception ex)
            {
                _trayIconService.ShowInfo($"{CommandDisplayName(command)} hotkey restore failed", ex.Message);
            }
        }
    }

    private static string CommandDisplayName(LookupCommand command)
    {
        return command switch
        {
            LookupCommand.Selection => "Selection",
            LookupCommand.ManualLookup => "Manual lookup",
            LookupCommand.RecentLookups => "Recent lookups",
            LookupCommand.ExplainSelection => "Text explanation",
            _ => command.ToString(),
        };
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
