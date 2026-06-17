namespace RealtimeTranslate.Core.Models;

public sealed record LookupPresentation(
    string Title,
    string Subtitle,
    IReadOnlyList<string> Definitions,
    bool IsError = false);
