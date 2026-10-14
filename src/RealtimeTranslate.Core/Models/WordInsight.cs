namespace RealtimeTranslate.Core.Models;

public sealed record WordInsight(
    string Breakdown,
    IReadOnlyList<string> Family,
    string ContextHint,
    string EvidenceSource = "",
    double Confidence = 0)
{
    public static WordInsight Empty { get; } = new(string.Empty, [], string.Empty);

    public bool HasEvidence => !string.IsNullOrWhiteSpace(EvidenceSource) && Confidence > 0;
}
