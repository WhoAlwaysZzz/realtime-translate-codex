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

    public static string ContextAlignment(LookupPresentation presentation) =>
        presentation.IsContextAligned ? "整句句意对齐：已用于筛选" : "整句句意对齐：未用于筛选";

    public static IReadOnlyList<string> VisibleLines(LookupPresentation presentation)
    {
        return new[]
        {
            $"{presentation.Title} {Frequency(presentation.FrequencyBand)}".Trim(),
            presentation.PrimaryDefinition,
            presentation.IsError ? string.Empty : ContextAlignment(presentation),
            Breakdown(presentation),
            Family(presentation),
            Usage(presentation),
        }.Where(line => line.Length > 0).ToArray();
    }
}
