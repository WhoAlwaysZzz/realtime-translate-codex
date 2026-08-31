namespace RealtimeTranslate.Core.Models;

public sealed record LookupPresentation(
    string Title,
    string Subtitle,
    IReadOnlyList<string> Definitions,
    string PrimaryDefinition,
    int QueryCount,
    bool IsError = false,
    string WordBreakdown = "",
    IReadOnlyList<string>? WordFamily = null,
    string ContextHint = "",
    int FrequencyBand = 0,
    ContextTrustStatus ContextStatus = ContextTrustStatus.Unavailable,
    string InsightEvidence = "")
{
    public bool IsContextAligned => ContextStatus == ContextTrustStatus.UsedForMeaning;
}
