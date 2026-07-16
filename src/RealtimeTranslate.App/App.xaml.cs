using System.IO;
using System.Net.Http;
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

namespace RealtimeTranslate.App;

public partial class App : System.Windows.Application
{
    private AppController? _controller;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var settings = AppSettings.CreateDefault();
            var aiSettingsStore = new AiExplanationSettingsStore(Path.Combine(settings.ArchiveRoot, "ai-settings.json"));
            aiSettingsStore.EnsureTemplateExists();
            var aiOptions = aiSettingsStore.Load();
            var bundledDictionaryPath = Path.Combine(AppContext.BaseDirectory, "dictionary.tsv");
            var userDictionaryPath = Path.Combine(settings.ArchiveRoot, "dictionary.tsv");
            var wordCardsPath = Path.Combine(AppContext.BaseDirectory, "Data", "word-cards.tsv");
            _controller = new AppController(
                settings,
                new WindowsSelectionService(),
                new WindowsScreenshotService(settings.ArchiveRoot),
                new WindowsOcrService(),
                new OcrTextComposer(),
                new BasicWordNormalizer(),
                new EmbeddedDictionaryService([bundledDictionaryPath, userDictionaryPath]),
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
