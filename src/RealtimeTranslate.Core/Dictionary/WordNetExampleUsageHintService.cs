using System.Text.RegularExpressions;
using RealtimeTranslate.Core.Models;

namespace RealtimeTranslate.Core.Dictionary;

public sealed partial class WordNetExampleUsageHintService : IUsageHintService
{
    public string GetHint(DictionaryLookupResult lookup, SenseRankingResult ranking, string targetWord)
    {
        var senses = ranking.SelectedSense is not null
            ? [ranking.SelectedSense]
            : lookup.Senses ?? [];
        var hints = senses
            .SelectMany(sense => sense.EnglishExamples ?? [])
            .Where(example => ContainsTarget(example, targetWord))
            .Select(example => ToShortHint(example, targetWord))
            .Where(hint => hint.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(2)
            .ToArray();
        if (hints.Length == 0)
        {
            hints = senses
                .SelectMany(sense => sense.EnglishDefinitions)
                .Select(definition => ToShortHint(definition, targetWord))
                .Where(hint => hint.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(1)
                .ToArray();
        }
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
        return word.Equals(targetWord, StringComparison.OrdinalIgnoreCase)
            || word.StartsWith(targetWord, StringComparison.OrdinalIgnoreCase)
            || (targetWord.EndsWith('e') && word.StartsWith(targetWord[..^1], StringComparison.OrdinalIgnoreCase));
    }

    [GeneratedRegex("[A-Za-z0-9]+(?:'[A-Za-z]+)?")]
    private static partial Regex WordToken();
}
