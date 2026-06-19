namespace RealtimeTranslate.Core.Models;

public sealed record TextExplanation(
    string SourceText,
    string Translation,
    IReadOnlyList<string> Notes,
    string Provider,
    string Model);
