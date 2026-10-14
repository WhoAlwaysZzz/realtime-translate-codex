namespace RealtimeTranslate.Core.Dictionary;

/// <summary>Study hint plus the evidence that was verified while constructing it.</summary>
public sealed record UsageHintResult(string Text, string EvidenceSource, double Confidence)
{
    public static UsageHintResult Empty { get; } = new(string.Empty, string.Empty, 0);

    public bool HasEvidence => !string.IsNullOrWhiteSpace(Text)
        && !string.IsNullOrWhiteSpace(EvidenceSource)
        && Confidence > 0;
}
