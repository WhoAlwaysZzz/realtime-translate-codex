using System.Text.Json;
using System.IO.Compression;
using RealtimeTranslate.Core.Dictionary;
using RealtimeTranslate.Core.Explanation;
using RealtimeTranslate.Core.History;
using RealtimeTranslate.Core.Models;
using RealtimeTranslate.Core.Normalization;
using RealtimeTranslate.Core.Ocr;
using RealtimeTranslate.Core.Presentation;
using RealtimeTranslate.Core.Recording;
using RealtimeTranslate.Core.Stats;

var tests = new (string Name, Action Body)[]
{
    ("normalizer handles punctuation and inflections", NormalizerHandlesPunctuationAndInflections),
    ("dictionary falls back from recognized word to lemma", DictionaryFallsBackFromRecognizedWordToLemma),
    ("dictionary covers common desktop and developer words", DictionaryCoversCommonDesktopAndDeveloperWords),
    ("bundled dictionary covers broad common words", BundledDictionaryCoversBroadCommonWords),
    ("structured dictionary retains senses and frequency", StructuredDictionaryRetainsSensesAndFrequency),
    ("context ranker selects or abstains honestly", ContextRankerSelectsOrAbstainsHonestly),
    ("rendered card uses frequency and short usage fallback", RenderedCardUsesFrequencyAndShortUsageFallback),
    ("recorder writes jsonl with screenshot and selection", RecorderWritesJsonlWithScreenshotAndSelection),
    ("query counter persists word counts", QueryCounterPersistsWordCounts),
    ("recent lookup store replays newest entries", RecentLookupStoreReplaysNewestEntries),
    ("recent lookup store completes lookup prefixes", RecentLookupStoreCompletesLookupPrefixes),
    ("overlay placement planner stacks rapid reservations", OverlayPlacementPlannerStacksRapidReservations),
    ("AI explanation service parses JSON response", AiExplanationServiceParsesJsonResponse),
    ("explanation recorder writes jsonl", ExplanationRecorderWritesJsonl),
    ("OCR text composer joins words into readable text", OcrTextComposerJoinsWordsIntoReadableText),
    ("word card index loads exact reviewed fields", WordCardIndexLoadsExactFields),
    ("word card evidence is surfaced with confidence", WordCardEvidenceIsSurfacedWithConfidence),
    ("word card regressions reject false families", WordCardRegressionsRejectFalseFamilies),
    ("random advanced word audit meets coverage target", RandomAdvancedWordAuditMeetsCoverageTarget),
    ("random rendered cards meet visible coverage target", RandomRenderedCardsMeetVisibleCoverageTarget),
};

if (args is ["--export-card-audit", var auditPath])
{
    ExportRenderedCardAudit(auditPath);
    return 0;
}

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
    AssertEqual("everlasting", normalizer.Normalize("everlasting").Candidates[0]);
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
        OcrCandidates: [new OcrCandidate("running", 0.91)],
        ContextText: "The hero is running toward the gate.");

    recorder.Record(record);

    var jsonl = Path.Combine(root, "records", "lookups.jsonl");
    AssertTrue(File.Exists(jsonl), "expected jsonl file to be created");
    var line = File.ReadLines(jsonl).Single();
    using var doc = JsonDocument.Parse(line);
    AssertEqual("running", doc.RootElement.GetProperty("recognizedText").GetString());
    AssertEqual("run", doc.RootElement.GetProperty("lemma").GetString());
    AssertEqual(10, doc.RootElement.GetProperty("selection").GetProperty("x").GetInt32());
    AssertEqual("The hero is running toward the gate.", doc.RootElement.GetProperty("contextText").GetString());
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

static void RecentLookupStoreCompletesLookupPrefixes()
{
    var root = Path.Combine(Path.GetTempPath(), "realtime-translate-tests", Guid.NewGuid().ToString("N"));
    var path = Path.Combine(root, "records", "recent-lookups.json");
    var store = new JsonRecentLookupStore(path, maxEntries: 6);

    store.Add(new LookupPresentation("process", "source", ["过程"], "过程", 1));
    store.Add(new LookupPresentation("project", "source", ["项目"], "项目", 1));
    store.Add(new LookupPresentation("prompt", "source", ["提示"], "提示", 1));
    store.Add(new LookupPresentation("project", "source", ["项目"], "项目", 2));
    store.Add(new LookupPresentation("product", "source", ["failure"], "failure", 0, IsError: true));

    AssertEqual("project", store.CompletePrefix("Pro"));
    AssertEqual("prompt", store.CompletePrefix("prom"));
    AssertEqual(null, store.CompletePrefix(""));
    AssertEqual(null, store.CompletePrefix("missing"));

    var reloaded = new JsonRecentLookupStore(path, maxEntries: 6);
    AssertEqual("project", reloaded.CompletePrefix("proj"));
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

static void AiExplanationServiceParsesJsonResponse()
{
    var responseJson = """
        {
          "choices": [
            {
              "message": {
                "content": "{\"translation\":\"这是中文翻译\",\"notes\":[\"note one\",\"note two\"]}"
              }
            }
          ]
        }
        """;
    using var httpClient = new HttpClient(new StaticResponseHandler(responseJson));
    var service = new OpenAiCompatibleTextExplanationService(
        httpClient,
        new OpenAiCompatibleTextExplanationOptions("test", "https://example.test", "test-model", "key"));

    var explanation = service.ExplainAsync("This is a sentence.").GetAwaiter().GetResult();

    AssertEqual("This is a sentence.", explanation.SourceText);
    AssertEqual("这是中文翻译", explanation.Translation);
    AssertEqual("note one", explanation.Notes[0]);
    AssertEqual("test-model", explanation.Model);
}

static void ExplanationRecorderWritesJsonl()
{
    var root = Path.Combine(Path.GetTempPath(), "realtime-translate-tests", Guid.NewGuid().ToString("N"));
    var recorder = new JsonlTextExplanationRecorder(root);
    var record = new TextExplanationRecord(
        OccurredAt: new DateTimeOffset(2026, 6, 19, 12, 0, 0, TimeSpan.Zero),
        SourceText: "This is a sentence.",
        Translation: "这是一个句子。",
        Notes: ["sentence: 句子"],
        Selection: new ScreenSelection(1, 2, 300, 80),
        ScreenshotPath: Path.Combine(root, "screenshots", "sample.png"),
        Provider: "test",
        Model: "test-model",
        ErrorMessage: null);

    recorder.Record(record);

    var jsonl = Path.Combine(root, "records", "explanations.jsonl");
    AssertTrue(File.Exists(jsonl), "expected explanations jsonl file to be created");
    using var doc = JsonDocument.Parse(File.ReadLines(jsonl).Single());
    AssertEqual("This is a sentence.", doc.RootElement.GetProperty("sourceText").GetString());
    AssertEqual("这是一个句子。", doc.RootElement.GetProperty("translation").GetString());
    AssertEqual(300, doc.RootElement.GetProperty("selection").GetProperty("width").GetInt32());
}

static void OcrTextComposerJoinsWordsIntoReadableText()
{
    var composer = new OcrTextComposer();
    var text = composer.Compose([
        new OcrCandidate("This", 1),
        new OcrCandidate("is", 1),
        new OcrCandidate("complex", 1),
        new OcrCandidate(",", 1),
        new OcrCandidate("isn't", 1),
        new OcrCandidate("it", 1),
        new OcrCandidate("?", 1),
    ]);

    AssertEqual("This is complex, isn't it?", text);
}

static void WordCardIndexLoadsExactFields()
{
    var root = CreateInsightFixture();
    var service = new MorphemeWordInsightService(Path.Combine(root, "word-cards.tsv"));

    var insight = service.Lookup("Ineffable");

    AssertEqual("ineffable（词基：无法形容的）", insight.Breakdown);
    AssertEqual("effable", insight.Family[0]);
    AssertEqual("sacred mysteries", insight.ContextHint);
    AssertEqual(WordInsight.Empty, service.Lookup("not-in-index"));
}

static void WordCardEvidenceIsSurfacedWithConfidence()
{
    var root = CreateInsightFixture();
    var service = new MorphemeWordInsightService(Path.Combine(root, "word-cards.tsv"));
    var insight = service.Lookup("ineffable");
    AssertTrue(insight.HasEvidence, "non-empty insight must carry evidence");
    AssertEqual("MorphoLex (2018)", insight.EvidenceSource);
    AssertTrue(insight.Confidence > 0.7, "reviewed insight should have usable confidence");

    var presentation = new LookupPresentation("ineffable", "lemma: ineffable", ["无法形容的"], "无法形容的", 1,
        WordBreakdown: insight.Breakdown, InsightEvidence: $"{insight.EvidenceSource}；置信度 {insight.Confidence:P0}");
    AssertTrue(LookupCardTextFormatter.VisibleLines(presentation).Any(line => line.StartsWith("证据  ", StringComparison.Ordinal)),
        "card should expose evidence separately from the gloss");
}

static void StructuredDictionaryRetainsSensesAndFrequency()
{
    var root = Path.Combine(Path.GetTempPath(), "realtime-translate-tests", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(root);
    var path = Path.Combine(root, "lexicon.jsonl.gz");
    using (var file = File.Create(path))
    using (var gzip = new GZipStream(file, CompressionMode.Compress))
    using (var writer = new StreamWriter(gzip))
    {
        writer.WriteLine("{\"w\":\"bark\",\"f\":2,\"s\":[{\"p\":\"n\",\"z\":\"树皮、吠声\",\"d\":[\"tough protective covering of trees\"],\"x\":[]},{\"p\":\"v\",\"z\":\"吠、叫骂\",\"d\":[\"make barking sounds\"],\"x\":[\"the dog began to bark\"]}]}");
    }

    var service = new StructuredDictionaryService(path, new EmbeddedDictionaryService());
    var lookup = service.Lookup(new NormalizedWord("bark", "bark", ["bark"]));

    AssertTrue(lookup.Found, "expected structured dictionary hit");
    AssertEqual(2, lookup.FrequencyBand);
    AssertEqual(2, lookup.Senses!.Count);
    AssertEqual("v", lookup.Senses[1].PartOfSpeech);
    AssertEqual("吠、叫骂", lookup.Senses[1].ShortDefinition);
    AssertTrue(lookup.Senses[1].EnglishDefinitions.Contains("make barking sounds"), "expected English evidence for ranking");
}

static void ContextRankerSelectsOrAbstainsHonestly()
{
    var senses = new[]
    {
        new DictionarySense("n", "树皮", ["tough protective covering of trees"], ["the bark protects the tree"]),
        new DictionarySense("v", "吠叫", ["make barking sounds"], ["the dog began to bark"]),
    };
    var lookup = new DictionaryLookupResult(true, "bark", ["n. 树皮", "v. 吠叫"], senses, 2);
    var ranker = new LocalContextSenseRanker();

    var dog = ranker.Rank(lookup, "bark", "The dog began to bark at the stranger.");
    AssertTrue(dog.IsConfident, "dog context should be confidently verbal");
    AssertTrue(dog.WasContextAligned, "dog context should be marked as aligned");
    AssertEqual("本句中，bark作动词，指“吠叫”。", dog.PrimaryDefinition);

    var tree = ranker.Rank(lookup, "bark", "The bark protects the old tree.");
    AssertTrue(tree.IsConfident, "article and tree context should be confidently nominal");
    AssertTrue(tree.WasContextAligned, "tree context should be marked as aligned");
    AssertEqual("本句中，bark作名词，指“树皮”。", tree.PrimaryDefinition);

    var frailLookup = new DictionaryLookupResult(true, "frail", ["adj. 脆弱的", "n. 灯心草篓"], [
        new DictionarySense("adj", "脆弱的", ["physically weak"]),
        new DictionarySense("n", "灯心草篓", ["a basket for holding dried fruit"]),
    ], 2);
    var frail = ranker.Rank(frailLookup, "frail", "She looks at the frail, slender girl in front of her.");
    AssertTrue(frail.IsConfident, "appositive adjective context should be confidently ranked");
    AssertEqual("本句中，frail作形容词，指“脆弱的”。", frail.PrimaryDefinition);

    var singleSense = new DictionaryLookupResult(true, "dwindle", ["v. 逐渐减少、缩小"], [
        new DictionarySense("v", "逐渐减少、缩小", ["become smaller or lose substance"]),
    ], 2);
    var single = ranker.Rank(singleSense, "dwindle", "Their savings dwindled away.");
    AssertEqual("本句中，dwindle作动词，指“逐渐减少、缩小”。", single.PrimaryDefinition);

    var staunchLookup = new DictionaryLookupResult(true, "staunch", ["v. 止住", "adj. 忠实"], [
        new DictionarySense("v", "止住", ["stop or check the flow of blood" ]),
        new DictionarySense("adj", "忠实", ["loyal and committed" ]),
    ], 2);
    var staunch = ranker.Rank(staunchLookup, "staunch", "He is an irritating defeatist and supporter of anocentrism.");
    AssertTrue(staunch.IsConfident, "masked adjective context should be confidently ranked");
    AssertEqual("本句中，staunch作形容词，指“忠实的”。", staunch.PrimaryDefinition);

    var genericSoldier = ranker.Rank(staunchLookup, "staunch", "No soldier of Zeno would take pride in an execution.");
    AssertTrue(!genericSoldier.IsConfident, "an unrelated generic soldier must not force an adjective sense");
    AssertTrue(genericSoldier.PrimaryDefinition.StartsWith("无法仅凭这一句确定；可能是：", StringComparison.Ordinal), "generic soldier context should explicitly abstain");

    var richBarkLookup = new DictionaryLookupResult(true, "bark", ["n. 树皮、吠声", "v. 吠、叫骂、喊出、剥树皮"], [
        new DictionarySense("n", "树皮、吠声", ["the sound made by a dog", "tough protective covering of trees"]),
        new DictionarySense("v", "吠、叫骂、喊出、剥树皮", ["make barking sounds"], ["The dogs barked at the stranger"]),
    ], 2);
    var conciseBark = ranker.Rank(richBarkLookup, "bark", "The dog began to bark");
    AssertEqual("本句中，bark作动词，指“吠叫”。", conciseBark.PrimaryDefinition);

    var richStaunchLookup = new DictionaryLookupResult(true, "staunch", ["v. 止住、止血", "adj. 坚固的、坚强的、忠实的"], [
        new DictionarySense("v", "止住、止血", ["stop the flow of a liquid"]),
        new DictionarySense("adj", "坚固的、坚强的、忠实的", ["firm and dependable especially in loyalty"]),
    ], 1);
    var conciseStaunch = ranker.Rank(richStaunchLookup, "staunch", "He is an irritating defeatist and supporter of anocentrism.");
    AssertEqual("本句中，staunch作形容词，指“忠实的”。", conciseStaunch.PrimaryDefinition);

    var conciseReviewed = new (string Word, string Pos, string Definition, string Expected)[]
    {
        ("fortress", "n", "城堡、要塞", "城堡"),
        ("burden", "n", "负担、重载、担子", "负担"),
        ("sarcasm", "n", "挖苦、讽刺、讽刺话", "讽刺"),
    };
    foreach (var sample in conciseReviewed)
    {
        var reviewedLookup = new DictionaryLookupResult(true, sample.Word, [$"{sample.Pos}. {sample.Definition}"], [
            new DictionarySense(sample.Pos, sample.Definition, ["reviewed game sense"]),
        ], 2);
        var reviewed = ranker.Rank(reviewedLookup, sample.Word, "The surrounding scene provides the word.");
        AssertEqual($"本句中，{sample.Word}作名词，指“{sample.Expected}”。", reviewed.PrimaryDefinition);
    }

    var parrot = ranker.Rank(lookup, "bark", "Marcus Gosh, she's babbling like a");
    AssertTrue(parrot.IsConfident, "a masked noun frame should select the noun sense");
    AssertEqual("本句中，bark作名词，指“树皮”。", parrot.PrimaryDefinition);

    var unknown = ranker.Rank(lookup, "bark", string.Empty);
    AssertTrue(!unknown.IsConfident, "missing context must not pretend to disambiguate");
    AssertTrue(!unknown.WasContextAligned, "missing context must not be marked as aligned");
    AssertTrue(unknown.PrimaryDefinition.StartsWith("无法仅凭这一句确定；可能是：", StringComparison.Ordinal), "ambiguous card should say that the available sentence cannot determine the sense");
    AssertTrue(unknown.PrimaryDefinition.Contains("① n. 树皮", StringComparison.Ordinal), "ambiguous card should flatten the first sense");
    AssertTrue(unknown.PrimaryDefinition.Contains("② v. 吠叫", StringComparison.Ordinal), "ambiguous card should flatten the second sense");

    var ambiguous = ranker.Rank(lookup, "bark", "They bark loudly.");
    AssertTrue(!ambiguous.IsConfident, "non-empty but unsupported context must not force a sense");
    AssertTrue(!ambiguous.WasContextAligned, "unsupported context must not be marked as aligned");
    AssertTrue(ambiguous.PrimaryDefinition.StartsWith("无法仅凭这一句确定；可能是：", StringComparison.Ordinal), "unsupported context should show the abstention explanation");

    var crackLookup = new DictionaryLookupResult(true, "crack", ["n. 裂缝", "v. 爆裂", "adj. 第一流的"], [
        new DictionarySense("n", "裂缝", ["a narrow opening"]),
        new DictionarySense("v", "爆裂", ["break suddenly"]),
        new DictionarySense("adj", "第一流的", ["of the highest quality"]),
    ], 2);
    var crack = ranker.Rank(crackLookup, "crack", "No soldier of Zeno would take pride in an execution.");
    AssertTrue(!crack.IsConfident, "unrelated context must not force crack's adjective sense");
    AssertTrue(crack.PrimaryDefinition.Contains("③ adj. 第一流的", StringComparison.Ordinal), "an abstaining card should retain the third concise candidate");
}

static void RenderedCardUsesFrequencyAndShortUsageFallback()
{
    var sense = new DictionarySense(
        "v",
        "逐渐减少、缩小",
        ["become smaller or lose substance"],
        ["their savings dwindled away", "our dwindling natural resources"]);
    var lookup = new DictionaryLookupResult(true, "dwindle", ["v. 逐渐减少、缩小"], [sense], 2);
    var ranking = new SenseRankingResult(sense.ShortDefinition, lookup.Definitions, true, sense);
    var hint = new WordNetExampleUsageHintService().GetHint(lookup, ranking, "dwindle");
    var presentation = new LookupPresentation(
        "dwindle",
        "lemma: dwindle",
        lookup.Definitions,
        ranking.PrimaryDefinition,
        1,
        WordBreakdown: string.Empty,
        WordFamily: ["dwindled", "dwindling"],
        ContextHint: hint,
        FrequencyBand: lookup.FrequencyBand);

    var lines = LookupCardTextFormatter.VisibleLines(presentation);
    AssertEqual("dwindle ★★☆", lines[0]);
    AssertEqual("逐渐减少、缩小", lines[1]);
    AssertEqual("整句句意对齐：未用于筛选", lines[2]);
    AssertEqual("词族/词形  dwindled · dwindling", lines[3]);
    AssertEqual("用法/语境  their savings dwindled away · our dwindling natural resources", lines[4]);

    var alignedLines = LookupCardTextFormatter.VisibleLines(presentation with { IsContextAligned = true });
    AssertEqual("整句句意对齐：已用于筛选", alignedLines[2]);

    var crowded = presentation with
    {
        PrimaryDefinition = "本句中，dwindle作动词，指“逐渐减少、缩小”。",
        WordBreakdown = "dwin-（减少）",
        WordFamily = ["dwindled", "dwindling"],
    };
    var crowdedLines = LookupCardTextFormatter.VisibleLines(crowded);
    AssertTrue(crowdedLines.Count <= 5, "first screen must contain at most five information blocks");
    AssertTrue(crowdedLines.Contains("本句中，dwindle作动词，指“逐渐减少、缩小”。"), "sentence meaning must remain visible on crowded cards");
    AssertTrue(crowdedLines.Contains("词基/构词  dwin-（减少）"), "breakdown should be the optional metadata block");
    AssertTrue(!crowdedLines.Any(line => line.StartsWith("词族/词形", StringComparison.Ordinal)), "word family should yield to breakdown on first screen");
}

static void WordCardRegressionsRejectFalseFamilies()
{
    var appDirectory = FindAppSourceDirectory();
    var service = new MorphemeWordInsightService(Path.Combine(appDirectory, "Data", "word-cards.tsv"));

    var videos = service.Lookup("videos");
    AssertTrue(videos.Breakdown.StartsWith("video（", StringComparison.Ordinal), "videos must be analyzed from video");
    AssertTrue(videos.Family.Contains("video"), "videos must link back to video");
    AssertTrue(!videos.Family.Contains("point") && !videos.Family.Contains("punch"), "videos must not contain unrelated family words");
    AssertTrue(!service.Lookup("point").Family.Contains("punch"), "point and punch are not a word family");
    AssertTrue(!service.Lookup("punch").Family.Contains("point"), "punch and point are not a word family");
    AssertEqual("in-（不） + effable（可表达的）", service.Lookup("ineffable").Breakdown);
    AssertEqual("bene-（善） + vol-（意愿） + -ent（具有……的）", service.Lookup("benevolent").Breakdown);
    AssertEqual(string.Empty, service.Lookup("incorporate").Breakdown);
    AssertEqual(string.Empty, service.Lookup("glossary").Breakdown);
    AssertEqual(string.Empty, service.Lookup("withhold").Breakdown);
}

static void RandomAdvancedWordAuditMeetsCoverageTarget()
{
    var appDirectory = FindAppSourceDirectory();
    var insights = new MorphemeWordInsightService(Path.Combine(appDirectory, "Data", "word-cards.tsv"));
    var words = File.ReadAllLines(Path.Combine(FindRepositoryRoot(), "tests", "Data", "advanced-word-audit-sample.txt"))
        .Where(line => line.Length > 0).ToArray();
    var withFamily = 0;
    var withMorphology = 0;

    AssertEqual(200, words.Length);
    foreach (var word in words)
    {
        var insight = insights.Lookup(word);
        AssertTrue(insight.Family.Distinct(StringComparer.OrdinalIgnoreCase).Count() == insight.Family.Count, $"duplicate family member for '{word}'");
        AssertTrue(!insight.Family.Contains(word, StringComparer.OrdinalIgnoreCase), $"family repeats queried word '{word}'");
        withFamily += insight.Family.Count > 0 ? 1 : 0;
        withMorphology += insight.Breakdown.Length > 0
            && !insight.Breakdown.Contains("词形变化", StringComparison.Ordinal) ? 1 : 0;
    }
    AssertTrue(withFamily >= 180, $"expected at least 90% family coverage, got {withFamily}/{words.Length}");
    AssertTrue(withMorphology >= 70, $"expected at least 35% reviewed morphology coverage, got {withMorphology}/{words.Length}");
}

static void RandomRenderedCardsMeetVisibleCoverageTarget()
{
    var appDirectory = FindAppSourceDirectory();
    var dictionary = new StructuredDictionaryService(
        Path.Combine(appDirectory, "Data", "lexicon.jsonl.gz"),
        new EmbeddedDictionaryService(Path.Combine(appDirectory, "dictionary.tsv")));
    var insights = new MorphemeWordInsightService(Path.Combine(appDirectory, "Data", "word-cards.tsv"));
    var ranker = new LocalContextSenseRanker();
    var usage = new WordNetExampleUsageHintService();
    var normalizer = new BasicWordNormalizer();
    var words = File.ReadAllLines(Path.Combine(FindRepositoryRoot(), "tests", "Data", "advanced-word-audit-sample.txt"))
        .Where(line => line.Length > 0).ToArray();
    var withLearningHook = 0;

    foreach (var word in words)
    {
        var lookup = dictionary.Lookup(normalizer.Normalize(word));
        AssertTrue(lookup.Found, $"expected structured lookup for rendered card '{word}'");
        var ranking = ranker.Rank(lookup, lookup.EntryWord, string.Empty);
        var insight = insights.Lookup(word);
        if (insight == WordInsight.Empty)
        {
            insight = insights.Lookup(lookup.EntryWord);
        }
        var hint = insight.ContextHint;
        if (hint.Length == 0 && insight.Breakdown.Length == 0)
        {
            hint = usage.GetHint(lookup, ranking, lookup.EntryWord);
        }
        var presentation = new LookupPresentation(
            word,
            $"lemma: {lookup.EntryWord}",
            ranking.Definitions,
            ranking.PrimaryDefinition,
            1,
            WordBreakdown: insight.Breakdown,
            WordFamily: insight.Family,
            ContextHint: hint,
            FrequencyBand: lookup.FrequencyBand);
        var visible = LookupCardTextFormatter.VisibleLines(presentation);

        AssertTrue(visible.Count >= 2, $"rendered card '{word}' has no definition");
        AssertTrue(visible[0].Contains('★'), $"rendered card '{word}' has no frequency band");
        AssertTrue(!string.Join(" | ", visible).Contains("词基：", StringComparison.Ordinal), $"rendered card '{word}' exposes a fake base line");
        withLearningHook += insight.Breakdown.Length > 0 || hint.Length > 0 ? 1 : 0;
    }

    AssertTrue(withLearningHook >= 190, $"expected at least 95% visible morphology-or-usage coverage, got {withLearningHook}/{words.Length}");
}

static void ExportRenderedCardAudit(string outputPath)
{
    var appDirectory = FindAppSourceDirectory();
    var dictionary = new StructuredDictionaryService(
        Path.Combine(appDirectory, "Data", "lexicon.jsonl.gz"),
        new EmbeddedDictionaryService(Path.Combine(appDirectory, "dictionary.tsv")));
    var insights = new MorphemeWordInsightService(Path.Combine(appDirectory, "Data", "word-cards.tsv"));
    var ranker = new LocalContextSenseRanker();
    var usage = new WordNetExampleUsageHintService();
    var normalizer = new BasicWordNormalizer();
    var words = File.ReadAllLines(Path.Combine(FindRepositoryRoot(), "tests", "Data", "advanced-word-audit-sample.txt"))
        .Where(line => line.Length > 0).ToArray();
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
    using var writer = new StreamWriter(outputPath, false, new System.Text.UTF8Encoding(false));
    writer.WriteLine("word\tvisible card lines");
    foreach (var word in words)
    {
        var lookup = dictionary.Lookup(normalizer.Normalize(word));
        var ranking = ranker.Rank(lookup, lookup.EntryWord, string.Empty);
        var insight = insights.Lookup(word);
        if (insight == WordInsight.Empty)
        {
            insight = insights.Lookup(lookup.EntryWord);
        }
        var hint = insight.ContextHint;
        if (hint.Length == 0 && insight.Breakdown.Length == 0)
        {
            hint = usage.GetHint(lookup, ranking, lookup.EntryWord);
        }
        var presentation = new LookupPresentation(
            word, $"lemma: {lookup.EntryWord}", ranking.Definitions, ranking.PrimaryDefinition, 1,
            WordBreakdown: insight.Breakdown, WordFamily: insight.Family,
            ContextHint: hint, FrequencyBand: lookup.FrequencyBand);
        writer.WriteLine($"{word}\t{string.Join(" | ", LookupCardTextFormatter.VisibleLines(presentation))}");
    }
}

static string CreateInsightFixture()
{
    var root = Path.Combine(Path.GetTempPath(), "realtime-translate-tests", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(root);
    File.WriteAllText(
        Path.Combine(root, "word-cards.tsv"),
        "ineffable\tineffable（词基：无法形容的）\teffable,ineffability\tsacred mysteries");
    return root;
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
    return Path.Combine(FindAppSourceDirectory(), "dictionary.tsv");
}

static string FindRepositoryRoot()
{
    return Directory.GetParent(Directory.GetParent(FindAppSourceDirectory())!.FullName)!.FullName;
}

static string FindAppSourceDirectory()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null)
    {
        var path = Path.Combine(directory.FullName, "src", "RealtimeTranslate.App");
        if (File.Exists(Path.Combine(path, "dictionary.tsv")))
        {
            return path;
        }

        directory = directory.Parent;
    }

    throw new DirectoryNotFoundException("Could not locate src/RealtimeTranslate.App.");
}

internal sealed class StaticResponseHandler : HttpMessageHandler
{
    private readonly string _responseBody;

    public StaticResponseHandler(string responseBody)
    {
        _responseBody = responseBody;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(_responseBody),
        };
        return Task.FromResult(response);
    }
}
