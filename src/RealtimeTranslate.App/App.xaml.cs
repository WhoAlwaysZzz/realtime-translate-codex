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
                new JsonlLookupRecorder(settings.ArchiveRoot),
                new JsonlTextExplanationRecorder(settings.ArchiveRoot),
                new JsonWordQueryCounter(Path.Combine(settings.ArchiveRoot, "records", "query-counts.json")),
                new UserPreferencesStore(Path.Combine(settings.ArchiveRoot, "settings.json")));
            _controller.Start();

            if (e.Args.Length >= 2 && string.Equals(e.Args[0], "--replay", StringComparison.OrdinalIgnoreCase))
            {
                var recordsPath = e.Args[1];
                var record = File.ReadLines(recordsPath)
                    .Select(line => JsonSerializer.Deserialize<LookupRecord>(line))
                    .FirstOrDefault(item => item is not null && !string.IsNullOrWhiteSpace(item.ScreenshotPath));
                if (record is null)
                {
                    throw new InvalidOperationException("No replayable lookup record was found.");
                }

                var capture = await new ReplayScreenshotService().CaptureAsync(record.ScreenshotPath, record.Selection);
                await _controller.ReplayAsync(capture);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "Realtime Translate Codex failed to start", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _controller?.Dispose();
        base.OnExit(e);
    }
}
