using System.Text.Json;
using RealtimeTranslate.Core.Dictionary;
using RealtimeTranslate.Core.History;
using RealtimeTranslate.Core.Models;
using RealtimeTranslate.Core.Normalization;
using RealtimeTranslate.Core.Presentation;
using RealtimeTranslate.Core.Recording;
using RealtimeTranslate.Core.Stats;

var tests = new (string Name, Action Body)[]
{
    ("normalizer handles punctuation and inflections", NormalizerHandlesPunctuationAndInflections),
    ("dictionary falls back from recognized word to lemma", DictionaryFallsBackFromRecognizedWordToLemma),
    ("dictionary covers common desktop and developer words", DictionaryCoversCommonDesktopAndDeveloperWords),
    ("bundled dictionary covers broad common words", BundledDictionaryCoversBroadCommonWords),
    ("recorder writes jsonl with screenshot and selection", RecorderWritesJsonlWithScreenshotAndSelection),
    ("query counter persists word counts", QueryCounterPersistsWordCounts),
    ("recent lookup store replays newest entries", RecentLookupStoreReplaysNewestEntries),
    ("overlay placement planner stacks rapid reservations", OverlayPlacementPlannerStacksRapidReservations),
};

var failed = 0;
foreach (var test in tests)
{
    try
    {
        test.Body();
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception ex)
    {
        failed++;
        Console.Error.WriteLine($"FAIL {test.Name}: {ex.Message}");
    }
}

return failed;

static void NormalizerHandlesPunctuationAndInflections()
{
    var normalizer = new BasicWordNormalizer();

    AssertEqual("guess", normalizer.Normalize("\"guessed!\"").Lemma);
    AssertEqual("child", normalizer.Normalize("children").Lemma);
    AssertEqual("run", normalizer.Normalize("running").Lemma);
    AssertEqual("future", normalizer.Normalize("Future.").Lemma);
}

static void DictionaryFallsBackFromRecognizedWordToLemma()
{
    var dictionary = new EmbeddedDictionaryService();
    var result = dictionary.Lookup(new NormalizedWord("guessed", "guess", ["guessed", "guess"]));

    AssertTrue(result.Found, "expected dictionary hit for lemma 'guess'");
    AssertEqual("guess", result.EntryWord);
    AssertTrue(result.Definitions.Any(d => d.Contains("猜", StringComparison.Ordinal)), "expected Chinese definition for guess");
}

static void DictionaryCoversCommonDesktopAndDeveloperWords()
{
    var dictionary = new EmbeddedDictionaryService();

    foreach (var word in new[] { "project", "dotnet", "realtime", "translate", "cache", "warning", "error" })
    {
        var result = dictionary.Lookup(new NormalizedWord(word, word, [word]));
        AssertTrue(result.Found, $"expected dictionary hit for '{word}'");
    }
}

static void BundledDictionaryCoversBroadCommonWords()
{
    var dictionaryPath = FindBundledDictionary();
    var dictionary = new EmbeddedDictionaryService(dictionaryPath);

    foreach (var word in new[] { "abandon", "ability", "contain", "necessary", "mysterious", "conversation", "evidence" })
    {
        var result = dictionary.Lookup(new NormalizedWord(word, word, [word]));
        AssertTrue(result.Found, $"expected bundled dictionary hit for '{word}'");
    }
}

static void RecorderWritesJsonlWithScreenshotAndSelection()
{
    var root = Path.Combine(Path.GetTempPath(), "realtime-translate-tests", Guid.NewGuid().ToString("N"));
    var recorder = new JsonlLookupRecorder(root);
    var record = new LookupRecord(
        OccurredAt: new DateTimeOffset(2026, 6, 17, 12, 0, 0, TimeSpan.Zero),
        RecognizedText: "running",
        DisplayWord: "running",
        Lemma: "run",
        Definitions: ["跑", "运行"],
        Selection: new ScreenSelection(10, 20, 120, 32),
        ScreenshotPath: Path.Combine(root, "screenshots", "sample.png"),
        OcrCandidates: [new OcrCandidate("running", 0.91)]);

    recorder.Record(record);

    var jsonl = Path.Combine(root, "records", "lookups.jsonl");
    AssertTrue(File.Exists(jsonl), "expected jsonl file to be created");
    var line = File.ReadLines(jsonl).Single();
    using var doc = JsonDocument.Parse(line);
    AssertEqual("running", doc.RootElement.GetProperty("recognizedText").GetString());
    AssertEqual("run", doc.RootElement.GetProperty("lemma").GetString());
    AssertEqual(10, doc.RootElement.GetProperty("selection").GetProperty("x").GetInt32());
    AssertTrue(
        doc.RootElement.GetProperty("screenshotPath").GetString()!.EndsWith("sample.png", StringComparison.Ordinal),
        "expected screenshot path to be serialized");
}

static void QueryCounterPersistsWordCounts()
{
    var root = Path.Combine(Path.GetTempPath(), "realtime-translate-tests", Guid.NewGuid().ToString("N"));
    var path = Path.Combine(root, "records", "query-counts.json");

    var counter = new JsonWordQueryCounter(path);
    AssertEqual(1, counter.Increment("Project"));
    AssertEqual(2, counter.Increment("project"));

    var reloaded = new JsonWordQueryCounter(path);
    AssertEqual(3, reloaded.Increment("project"));
}

static void RecentLookupStoreReplaysNewestEntries()
{
    var root = Path.Combine(Path.GetTempPath(), "realtime-translate-tests", Guid.NewGuid().ToString("N"));
    var path = Path.Combine(root, "records", "recent-lookups.json");
    var store = new JsonRecentLookupStore(path, maxEntries: 3);

    store.Add(new LookupPresentation("first", "source", ["一"], "一", 1));
    store.Add(new LookupPresentation("second", "source", ["二"], "二", 1));
    store.Add(new LookupPresentation("third", "source", ["三"], "三", 1));
    store.Add(new LookupPresentation("fourth", "source", ["四"], "四", 1));

    var recent = store.GetRecent(3);
    AssertEqual(3, recent.Count);
    AssertEqual("fourth", recent[0].Title);
    AssertEqual("third", recent[1].Title);
    AssertEqual("second", recent[2].Title);

    var reloaded = new JsonRecentLookupStore(path, maxEntries: 3);
    AssertEqual("fourth", reloaded.GetRecent(1)[0].Title);
}

static void OverlayPlacementPlannerStacksRapidReservations()
{
    var planner = new OverlayPlacementPlanner();
    var workArea = new OverlayWorkArea(0, 0, 1920, 1080);

    var first = planner.Reserve(workArea, 92);
    var second = planner.Reserve(workArea, 92);
    var third = planner.Reserve(workArea, 92);

    AssertEqual(22d, first.Top);
    AssertEqual(126d, second.Top);
    AssertEqual(230d, third.Top);
    AssertTrue(first.ToLeft == second.ToLeft && second.ToLeft == third.ToLeft, "expected replay windows to stay on the same side before wrapping");
}

static void AssertEqual<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"expected '{expected}', got '{actual}'");
    }
}

static void AssertTrue(bool value, string message)
{
    if (!value)
    {
        throw new InvalidOperationException(message);
    }
}

static string FindBundledDictionary()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null)
    {
        var path = Path.Combine(directory.FullName, "src", "RealtimeTranslate.App", "dictionary.tsv");
        if (File.Exists(path))
        {
            return path;
        }

        directory = directory.Parent;
    }

    throw new FileNotFoundException("Could not locate src/RealtimeTranslate.App/dictionary.tsv");
}
