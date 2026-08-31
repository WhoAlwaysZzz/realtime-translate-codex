using RealtimeTranslate.Core.Models;

namespace RealtimeTranslate.Core.Presentation;

public static class LookupCardTextFormatter
{
    public static string Frequency(int band) => band switch
    {
        3 => "★★★",
        2 => "★★☆",
        1 => "★☆☆",
        _ => string.Empty,
    };

    public static string Breakdown(LookupPresentation presentation) =>
        string.IsNullOrWhiteSpace(presentation.WordBreakdown) ? string.Empty : $"词基/构词  {presentation.WordBreakdown}";

    public static string Family(LookupPresentation presentation) => presentation.WordFamily is { Count: > 0 }
        ? $"词族/词形  {string.Join(" · ", presentation.WordFamily.Take(3))}"
        : string.Empty;

    public static string Usage(LookupPresentation presentation) =>
        string.IsNullOrWhiteSpace(presentation.ContextHint) ? string.Empty : $"用法/语境  {presentation.ContextHint}";

    public static string Evidence(LookupPresentation presentation) =>
        string.IsNullOrWhiteSpace(presentation.InsightEvidence)
            ? string.Empty
            : $"证据  {presentation.InsightEvidence}";

    public static string ContextTrust(LookupPresentation presentation) => presentation.ContextStatus switch
    {
        ContextTrustStatus.UsedForMeaning => "上下文：可信，已用于锁定本句义",
        ContextTrustStatus.TrustedNotUsed => "上下文：可信，未参与义项筛选",
        ContextTrustStatus.Rejected => "上下文：OCR不可靠，未参与释义",
        _ => "上下文：未识别到可用句子",
    };

    public static IReadOnlyList<string> VisibleLines(LookupPresentation presentation)
    {
        var core = new[]
        {
            $"{presentation.Title} {Frequency(presentation.FrequencyBand)}".Trim(),
            presentation.PrimaryDefinition,
            presentation.IsError ? string.Empty : ContextTrust(presentation),
        }.Where(line => line.Length > 0).ToList();

        // Keep the first screen compact: sentence meaning and usage outrank
        // study metadata. Show at most one optional metadata block so the
        // card never grows beyond five visible information blocks.
        var metadata = Breakdown(presentation);
        if (string.IsNullOrWhiteSpace(metadata))
        {
            metadata = Family(presentation);
        }

        if (!string.IsNullOrWhiteSpace(metadata))
        {
            core.Add(metadata);
        }

        var evidence = Evidence(presentation);
        if (!string.IsNullOrWhiteSpace(evidence))
        {
            core.Add(evidence);
        }

        // Provenance is part of the trust contract for learning content. When
        // the five-block first screen is full, keep the source-bearing line
        // and let the optional usage example yield space for it.
        var usage = Usage(presentation);
        if (!string.IsNullOrWhiteSpace(usage) && core.Count < 5)
        {
            core.Add(usage);
        }

        return core.Take(5).ToArray();
    }
}
