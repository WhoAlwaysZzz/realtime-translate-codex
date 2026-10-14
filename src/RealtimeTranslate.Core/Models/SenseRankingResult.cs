namespace RealtimeTranslate.Core.Models;

public sealed record SenseRankingResult(
    string PrimaryDefinition,
    IReadOnlyList<string> Definitions,
    bool IsConfident,
    DictionarySense? SelectedSense = null,
    ContextTrustStatus ContextStatus = ContextTrustStatus.Unavailable)
{
    public bool WasContextAligned => ContextStatus == ContextTrustStatus.UsedForMeaning;
}
