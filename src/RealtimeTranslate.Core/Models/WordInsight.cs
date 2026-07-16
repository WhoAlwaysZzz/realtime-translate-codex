namespace RealtimeTranslate.Core.Models;

public sealed record WordInsight(
    string Breakdown,
    IReadOnlyList<string> Family,
    string ContextHint)
{
    public static WordInsight Empty { get; } = new(string.Empty, [], string.Empty);
}
