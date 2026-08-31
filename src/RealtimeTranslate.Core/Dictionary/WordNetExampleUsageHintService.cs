using System.Text.RegularExpressions;
using RealtimeTranslate.Core.Models;

namespace RealtimeTranslate.Core.Dictionary;

public sealed partial class WordNetExampleUsageHintService : IUsageHintService
{
    public string GetHint(DictionaryLookupResult lookup, SenseRankingResult ranking, string targetWord)
    {
        // A usage example is a sense-specific learning claim.  When ranking
        // abstains, examples from all dictionary senses can silently steer the
        // learner toward one of the meanings that the main card refused to
        // choose.  Keep the same trust boundary for both layers: no selected
        // sense means no usage hint.
        if (ranking.SelectedSense is null)
        {
            return string.Empty;
        }

        var senses = new[] { ranking.SelectedSense };
        var hints = senses
            .SelectMany(sense => sense.EnglishExamples ?? [])
            .Where(example => ContainsTarget(example, targetWord))
            .Select(example => ToShortHint(example, targetWord))
            .Where(hint => hint.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(2)
            .ToArray();
        return string.Join(" · ", hints);
    }

    private static string ToShortHint(string example, string targetWord)
    {
        var words = WordToken().Matches(example).Select(match => match.Value).ToArray();
        if (words.Length == 0)
        {
            return string.Empty;
        }

        var targetIndex = Array.FindIndex(words, word => IsTargetForm(word, targetWord));
        var start = targetIndex < 0 ? 0 : Math.Max(0, targetIndex - 3);
        var end = targetIndex < 0 ? Math.Min(words.Length, 7) : Math.Min(words.Length, targetIndex + 4);
        var prefix = start > 0 ? "…" : string.Empty;
        var suffix = end < words.Length ? "…" : string.Empty;
        return $"{prefix}{string.Join(" ", words[start..end]).ToLowerInvariant()}{suffix}";
    }

    private static bool ContainsTarget(string text, string targetWord) =>
        WordToken().Matches(text).Any(match => IsTargetForm(match.Value, targetWord));

    private static bool IsTargetForm(string word, string targetWord)
    {
        if (word.Equals(targetWord, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Do not let a prefix match turn "go" into evidence for "good".
        // This is intentionally a small inflection whitelist rather than a
        // general stemmer: a displayed example must visibly contain the word
        // or a conventional surface inflection of it.
        if (word.StartsWith(targetWord, StringComparison.OrdinalIgnoreCase)
            && IsInflectionSuffix(word[targetWord.Length..]))
        {
            return true;
        }

        return targetWord.EndsWith('e')
            && word.StartsWith(targetWord[..^1], StringComparison.OrdinalIgnoreCase)
            && IsInflectionSuffix(word[(targetWord.Length - 1)..]);
    }

    private static bool IsInflectionSuffix(string suffix) => suffix.Equals("s", StringComparison.OrdinalIgnoreCase)
        || suffix.Equals("es", StringComparison.OrdinalIgnoreCase)
        || suffix.Equals("d", StringComparison.OrdinalIgnoreCase)
        || suffix.Equals("ed", StringComparison.OrdinalIgnoreCase)
        || suffix.Equals("ing", StringComparison.OrdinalIgnoreCase)
        || suffix.Equals("er", StringComparison.OrdinalIgnoreCase)
        || suffix.Equals("est", StringComparison.OrdinalIgnoreCase);

    [GeneratedRegex("[A-Za-z0-9]+(?:'[A-Za-z]+)?")]
    private static partial Regex WordToken();
}
