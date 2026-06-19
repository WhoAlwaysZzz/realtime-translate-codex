namespace RealtimeTranslate.Core.Models;

public sealed record TextExplanationPresentation(
    string Title,
    string SourceText,
    string Translation,
    IReadOnlyList<string> Notes,
    string Footer,
    bool IsError = false);
