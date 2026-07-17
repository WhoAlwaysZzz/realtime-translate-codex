namespace RealtimeTranslate.Core.Models;

public sealed record SenseRankingResult(
    string PrimaryDefinition,
    IReadOnlyList<string> Definitions,
    bool IsConfident);
