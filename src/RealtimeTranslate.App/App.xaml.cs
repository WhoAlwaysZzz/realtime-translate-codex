using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using RealtimeTranslate.App.Capture;
using RealtimeTranslate.App.Ocr;
using RealtimeTranslate.App.Presentation;
using RealtimeTranslate.App.Selection;
using RealtimeTranslate.App.Settings;
using RealtimeTranslate.Core.Dictionary;
using RealtimeTranslate.Core.Explanation;
using RealtimeTranslate.Core.History;
using RealtimeTranslate.Core.Normalization;
using RealtimeTranslate.Core.Ocr;
using RealtimeTranslate.Core.Recording;
using RealtimeTranslate.Core.Stats;
using RealtimeTranslate.Core.Models;

namespace RealtimeTranslate.App;

public partial class App : System.Windows.Application
{
    private AppController? _controller;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var replayMode = e.Args.Length >= 2 && string.Equals(e.Args[0], "--replay", StringComparison.OrdinalIgnoreCase);
            var desktopTestHost = e.Args.Any(arg => string.Equals(arg, "--desktop-test-host", StringComparison.OrdinalIgnoreCase));
            var settings = AppSettings.CreateDefault();
            var aiSettingsStore = new AiExplanationSettingsStore(Path.Combine(settings.ArchiveRoot, "ai-settings.json"));
            aiSettingsStore.EnsureTemplateExists();
            var aiOptions = aiSettingsStore.Load();
            var bundledDictionaryPath = Path.Combine(AppContext.BaseDirectory, "dictionary.tsv");
            var structuredDictionaryPath = Path.Combine(AppContext.BaseDirectory, "Data", "lexicon.jsonl.gz");
            var userDictionaryPath = Path.Combine(settings.ArchiveRoot, "dictionary.tsv");
            var wordCardsPath = Path.Combine(AppContext.BaseDirectory, "Data", "word-cards.tsv");
            _controller = new AppController(
                settings,
                new WindowsSelectionService(),
                new WindowsScreenshotService(settings.ArchiveRoot),
                new WindowsOcrService(),
                new OcrTextComposer(),
                new BasicWordNormalizer(),
                new StructuredDictionaryService(
                    structuredDictionaryPath,
                    new EmbeddedDictionaryService([bundledDictionaryPath, userDictionaryPath])),
                new LocalContextSenseRanker(),
                new WordNetExampleUsageHintService(),
                new MorphemeWordInsightService(wordCardsPath),
                new WpfOverlayPresenter(),
                new WpfTextExplanationPresenter(),
                new OpenAiCompatibleTextExplanationService(new HttpClient(), aiOptions),
                new JsonRecentLookupStore(Path.Combine(settings.ArchiveRoot, "records", "recent-lookups.json")),
                replayMode
                    ? new JsonlLookupRecorder(Path.Combine(Path.GetTempPath(), "RealtimeTranslateCodex-replay"))
                    : new JsonlLookupRecorder(settings.ArchiveRoot),
                new JsonlTextExplanationRecorder(settings.ArchiveRoot),
                new JsonWordQueryCounter(Path.Combine(settings.ArchiveRoot, "records", "query-counts.json")),
                new UserPreferencesStore(Path.Combine(settings.ArchiveRoot, "settings.json")));
            _controller.Start(!replayMode, registerHotkeys: !desktopTestHost);

            if (desktopTestHost && !replayMode)
            {
                var host = new MainWindow(isDesktopTestHost: true);
                host.Show();
                host.Activate();
            }

            if (replayMode)
            {
                var recordsPath = e.Args[1];
                var replayIndex = e.Args.Length >= 4 && int.TryParse(e.Args[3], out var parsedIndex) ? parsedIndex : 0;
                var replayDelayMilliseconds = e.Args.Length >= 6 && int.TryParse(e.Args[5], out var parsedDelay)
                    ? Math.Max(0, parsedDelay)
                    : 8000;
                var record = File.ReadLines(recordsPath)
                    .Select(ParseReplayRecord)
                    .Where(item => item is not null && !string.IsNullOrWhiteSpace(item.ScreenshotPath))
                    .Skip(replayIndex)
                    .FirstOrDefault();
                if (record is null)
                {
                    throw new InvalidOperationException("No replayable lookup record was found.");
                }

                var capture = await new ReplayScreenshotService().CaptureAsync(record.ScreenshotPath, record.Selection);
                await _controller.ReplayAsync(capture);
                await Task.Delay(replayDelayMilliseconds);
                Shutdown();
            }
        }
        catch (Exception ex)
        {
            try { File.WriteAllText(Path.Combine(Path.GetTempPath(), "RealtimeTranslateCodex-startup-error.txt"), ex.ToString()); } catch { }
            System.Windows.MessageBox.Show(ex.Message, "Realtime Translate Codex failed to start", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private static LookupRecord? ParseReplayRecord(string line)
    {
        using var json = JsonDocument.Parse(line);
        var root = json.RootElement;
        if (!root.TryGetProperty("screenshotPath", out var path) || string.IsNullOrWhiteSpace(path.GetString())) return null;
        var s = root.GetProperty("selection");
        return new LookupRecord(
            root.GetProperty("occurredAt").GetDateTimeOffset(),
            root.TryGetProperty("recognizedText", out var recognized) ? recognized.GetString() ?? string.Empty : string.Empty,
            root.TryGetProperty("displayWord", out var display) ? display.GetString() ?? string.Empty : string.Empty,
            root.TryGetProperty("lemma", out var lemma) ? lemma.GetString() ?? string.Empty : string.Empty,
            root.TryGetProperty("definitions", out var defs) ? defs.EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToArray() : [],
            new ScreenSelection(s.GetProperty("x").GetInt32(), s.GetProperty("y").GetInt32(), s.GetProperty("width").GetInt32(), s.GetProperty("height").GetInt32()),
            path.GetString()!,
            []);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _controller?.Dispose();
        base.OnExit(e);
    }
}
