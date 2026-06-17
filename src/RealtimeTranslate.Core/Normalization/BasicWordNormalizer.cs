using System.Text.RegularExpressions;
using RealtimeTranslate.Core.Models;

namespace RealtimeTranslate.Core.Normalization;

public sealed partial class BasicWordNormalizer : IWordNormalizer
{
    private static readonly IReadOnlyDictionary<string, string> IrregularLemmas =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["children"] = "child",
            ["men"] = "man",
            ["women"] = "woman",
            ["teeth"] = "tooth",
            ["feet"] = "foot",
            ["mice"] = "mouse",
            ["geese"] = "goose",
            ["went"] = "go",
            ["gone"] = "go",
            ["saw"] = "see",
            ["seen"] = "see",
            ["was"] = "be",
            ["were"] = "be",
            ["been"] = "be",
        };

    public NormalizedWord Normalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return NormalizedWord.Empty;
        }

        var match = EnglishWordRegex().Matches(text)
            .OrderByDescending(m => m.Value.Length)
            .FirstOrDefault();

        if (match is null)
        {
            return NormalizedWord.Empty;
        }

        var displayWord = match.Value.Trim('\'').ToLowerInvariant();
        if (displayWord.Length == 0)
        {
            return NormalizedWord.Empty;
        }

        var candidates = BuildCandidates(displayWord);
        return new NormalizedWord(displayWord, candidates[0], candidates);
    }

    private static IReadOnlyList<string> BuildCandidates(string word)
    {
        var candidates = new List<string>();
        AddCandidate(candidates, Lemmatize(word));
        AddCandidate(candidates, word);
        return candidates;
    }

    private static string Lemmatize(string word)
    {
        if (IrregularLemmas.TryGetValue(word, out var irregular))
        {
            return irregular;
        }

        if (word.Length > 5 && word.EndsWith("ies", StringComparison.Ordinal))
        {
            return word[..^3] + "y";
        }

        if (word.Length > 4 && word.EndsWith("ing", StringComparison.Ordinal))
        {
            return TrimDoubledFinalConsonant(word[..^3]);
        }

        if (word.Length > 3 && word.EndsWith("ied", StringComparison.Ordinal))
        {
            return word[..^3] + "y";
        }

        if (word.Length > 3 && word.EndsWith("ed", StringComparison.Ordinal))
        {
            return TrimDoubledFinalConsonant(word[..^2]);
        }

        if (word.Length > 4 && word.EndsWith("es", StringComparison.Ordinal))
        {
            return word[..^2];
        }

        if (word.Length > 3 && word.EndsWith("s", StringComparison.Ordinal) && !word.EndsWith("ss", StringComparison.Ordinal))
        {
            return word[..^1];
        }

        return word;
    }

    private static string TrimDoubledFinalConsonant(string word)
    {
        if (word.Length < 2)
        {
            return word;
        }

        var last = word[^1];
        var previous = word[^2];
        if (last == previous && last != 's' && !"aeiou".Contains(last, StringComparison.Ordinal))
        {
            return word[..^1];
        }

        return word;
    }

    private static void AddCandidate(List<string> candidates, string candidate)
    {
        if (!string.IsNullOrWhiteSpace(candidate) && !candidates.Contains(candidate, StringComparer.Ordinal))
        {
            candidates.Add(candidate);
        }
    }

    [GeneratedRegex("[A-Za-z]+(?:'[A-Za-z]+)?", RegexOptions.CultureInvariant)]
    private static partial Regex EnglishWordRegex();
}
