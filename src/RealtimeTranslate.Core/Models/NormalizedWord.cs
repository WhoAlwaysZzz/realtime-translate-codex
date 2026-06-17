namespace RealtimeTranslate.Core.Models;

public sealed record NormalizedWord(string DisplayWord, string Lemma, IReadOnlyList<string> Candidates)
{
    public static NormalizedWord Empty { get; } = new(string.Empty, string.Empty, []);
}
