using System.Text.Json;
using RealtimeTranslate.Core.Dictionary;
using RealtimeTranslate.Core.Models;
using RealtimeTranslate.Core.Normalization;
using RealtimeTranslate.Core.Recording;

var tests = new (string Name, Action Body)[]
{
    ("normalizer handles punctuation and inflections", NormalizerHandlesPunctuationAndInflections),
    ("dictionary falls back from recognized word to lemma", DictionaryFallsBackFromRecognizedWordToLemma),
    ("recorder writes jsonl with screenshot and selection", RecorderWritesJsonlWithScreenshotAndSelection),
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
