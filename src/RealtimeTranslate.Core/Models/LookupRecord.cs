namespace RealtimeTranslate.Core.Models;

public sealed record LookupRecord(
    DateTimeOffset OccurredAt,
    string RecognizedText,
    string DisplayWord,
    string Lemma,
    IReadOnlyList<string> Definitions,
    ScreenSelection Selection,
    string ScreenshotPath,
    IReadOnlyList<OcrCandidate> OcrCandidates,
    string ContextText = "",
    string PrimaryDefinition = "");
