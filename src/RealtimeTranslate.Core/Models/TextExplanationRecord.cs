namespace RealtimeTranslate.Core.Models;

public sealed record TextExplanationRecord(
    DateTimeOffset OccurredAt,
    string SourceText,
    string Translation,
    IReadOnlyList<string> Notes,
    ScreenSelection Selection,
    string ScreenshotPath,
    string Provider,
    string Model,
    string? ErrorMessage);
