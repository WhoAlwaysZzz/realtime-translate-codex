using System.Text.RegularExpressions;
using RealtimeTranslate.Core.Models;

namespace RealtimeTranslate.Core.Dictionary;

public sealed partial class LocalContextSenseRanker : ISenseRanker
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "and", "are", "as", "at", "be", "been", "by", "for", "from", "had", "has", "have",
        "he", "her", "his", "i", "in", "is", "it", "its", "of", "on", "or", "she", "that", "the",
        "their", "them", "they", "this", "to", "was", "were", "with", "you", "your",
    };

    private static readonly HashSet<string> VerbSignals = new(StringComparer.OrdinalIgnoreCase)
    {
        "to", "can", "could", "may", "might", "must", "shall", "should", "will", "would",
    };

    public SenseRankingResult Rank(DictionaryLookupResult lookup, string targetWord, string context)
    {
        var senses = lookup.Senses ?? [];
        if (senses.Count == 0)
        {
            var definitions = lookup.Definitions.ToArray();
            return new SenseRankingResult(definitions.FirstOrDefault() ?? string.Empty, definitions, definitions.Length <= 1);
        }

        if (senses.Count == 1)
        {
            return new SenseRankingResult(senses[0].ShortDefinition, [Format(senses[0])], true, senses[0]);
        }

        var contextTokens = Tokens(context).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var ordered = senses
            .Select((sense, index) => new RankedSense(sense, Score(sense, targetWord, context, contextTokens), index))
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Index)
            .ToArray();
        var confident = contextTokens.Count > 0
            && ordered[0].Score >= 2
            && ordered[0].Score - ordered[1].Score >= 1;

        if (confident)
        {
            return new SenseRankingResult(
                ordered[0].Sense.ShortDefinition,
                ordered.Select(item => Format(item.Sense)).ToArray(),
                true,
                ordered[0].Sense,
                WasContextAligned: true);
        }

        var flat = string.Join("  ", senses.Take(3).Select((sense, index) => $"{Number(index)} {Format(sense)}"));
        return new SenseRankingResult(flat, senses.Select(Format).ToArray(), false);
    }

    private static int Score(DictionarySense sense, string targetWord, string context, HashSet<string> contextTokens)
    {
        var score = 0;
        var words = WordToken().Matches(context).Select(match => match.Value).ToArray();
        for (var index = 0; index < words.Length; index++)
        {
            if (!words[index].Equals(targetWord, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var previous = index > 0 ? words[index - 1] : string.Empty;
            if (VerbSignals.Contains(previous) && sense.PartOfSpeech == "v")
            {
                score += 4;
            }
            if ((previous.Equals("a", StringComparison.OrdinalIgnoreCase)
                || previous.Equals("an", StringComparison.OrdinalIgnoreCase)
                || previous.Equals("the", StringComparison.OrdinalIgnoreCase))
                && sense.PartOfSpeech == "n")
            {
                score += 4;
            }
        }

        var definitionTokens = sense.EnglishDefinitions.SelectMany(Tokens).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var exampleTokens = (sense.EnglishExamples ?? []).SelectMany(Tokens).ToHashSet(StringComparer.OrdinalIgnoreCase);
        score += contextTokens.Count(token => definitionTokens.Contains(token));
        score += 2 * contextTokens.Count(token => exampleTokens.Contains(token));
        return score;
    }

    private static IEnumerable<string> Tokens(string text)
    {
        return WordToken().Matches(text)
            .Select(match => match.Value.ToLowerInvariant())
            .Where(token => token.Length > 1 && !StopWords.Contains(token));
    }

    private static string Format(DictionarySense sense) => string.IsNullOrWhiteSpace(sense.PartOfSpeech)
        ? sense.ShortDefinition
        : $"{sense.PartOfSpeech}. {sense.ShortDefinition}";

    private static string Number(int index) => index switch { 0 => "①", 1 => "②", _ => "③" };

    private sealed record RankedSense(DictionarySense Sense, int Score, int Index);

    [GeneratedRegex("[A-Za-z]+(?:'[A-Za-z]+)?")]
    private static partial Regex WordToken();
}
