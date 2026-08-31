using System.Text.RegularExpressions;
using RealtimeTranslate.Core.Models;

namespace RealtimeTranslate.Core.Dictionary;

public sealed partial class WordNetExampleUsageHintService : IUsageHintService
{
    public UsageHintResult GetHint(DictionaryLookupResult lookup, SenseRankingResult ranking, string targetWord)
    {
        // An abstaining ranker may still expose a study-only comparison, but
        // every example must remain attached to its own sense. This keeps the
        // main definition undecided while giving the learner a useful way to
        // distinguish the candidates.
        var isMultiSenseComparison = ranking.SelectedSense is null;
        var senses = isMultiSenseComparison
            ? lookup.Senses ?? []
            : new[] { ranking.SelectedSense! };

        // A partial comparison is not neutral: displaying evidence for only
        // one candidate can quietly recreate the sense choice that the main
        // card deliberately abstained from. Restrict this study-only form to
        // a complete, compact two-way contrast.
        if (isMultiSenseComparison && senses.Count != 2)
        {
            return UsageHintResult.Empty;
        }

        var hints = senses
            .Select(sense =>
            {
                var examples = (sense.EnglishExamples ?? [])
                    .Where(candidate => ContainsTarget(candidate, targetWord))
                    .Select(candidate => ToShortHint(candidate, targetWord))
                    .Where(hint => hint.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(2)
                    .ToArray();
                if (examples.Length == 0)
                {
                    return (Text: string.Empty, Examples: Array.Empty<string>());
                }

                var text = isMultiSenseComparison
                    ? $"{sense.PartOfSpeech}. {sense.ShortDefinition}：{string.Join(" · ", examples)}"
                    : string.Join(" · ", examples);
                return (Text: text, Examples: examples);
            })
            .Where(hint => hint.Text.Length > 0)
            .GroupBy(hint => hint.Text, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Take(2)
            .ToArray();
        if (isMultiSenseComparison && hints.Length != senses.Count)
        {
            return UsageHintResult.Empty;
        }
        if (hints.Length == 0)
        {
            // A single-sense entry may have no examples but still carry a
            // useful, source-backed English concept definition. Keep this
            // fallback bounded and study-only; never use it to choose among
            // competing senses.
            if (!isMultiSenseComparison && senses.Count == 1)
            {
                var definition = (senses[0].EnglishDefinitions ?? [])
                    .Select(CleanDefinition)
                    .FirstOrDefault(candidate => candidate.Length is >= 20 and <= 160);
                if (!string.IsNullOrWhiteSpace(definition))
                {
                    return new UsageHintResult(
                        $"核心概念  {definition}",
                        "Open English WordNet 2025；定义",
                        0.80);
                }
            }

            return UsageHintResult.Empty;
        }

        var examples = hints.SelectMany(hint => hint.Examples).ToArray();
        var exactCoverage = examples.Count(example => HasExactTarget(example, targetWord)) / (double)examples.Length;
        var confidence = 0.75 + exactCoverage * 0.15;
        var verification = exactCoverage == 1
            ? "例句直含目标词"
            : "例句含常规词形";
        return new UsageHintResult(
            string.Join(" · ", hints.Select(hint => hint.Text)),
            $"Open English WordNet 2025；{verification}",
            confidence);
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

    private static bool HasExactTarget(string text, string targetWord) =>
        WordToken().Matches(text).Any(match => match.Value.Equals(targetWord, StringComparison.OrdinalIgnoreCase));

    private static string CleanDefinition(string value) =>
        Regex.Replace(value.Trim(), "\\s+", " ").Trim(' ', '.', ';', ':');

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
