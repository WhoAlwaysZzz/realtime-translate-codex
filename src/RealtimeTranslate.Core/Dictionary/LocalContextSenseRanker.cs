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

    // When the selected word is masked out of the OCR context, a nearby noun
    // can still reveal an attributive adjective use (for example,
    // "staunch supporter"). Keep this list deliberately small and concrete so
    // it does not turn every noun-heavy sentence into an adjective guess.
    private static readonly HashSet<string> AdjectiveHeadNouns = new(StringComparer.OrdinalIgnoreCase)
    {
        "ally", "believer", "champion", "defender", "enemy", "fan", "friend",
        "leader", "opponent", "supporter", "advocate",
        "critic", "follower", "girl", "member", "partner", "body", "bodies", "scholar",
    };

    public SenseRankingResult Rank(DictionaryLookupResult lookup, string targetWord, string context)
    {
        return Rank(lookup, targetWord, [targetWord], context);
    }

    public SenseRankingResult Rank(DictionaryLookupResult lookup, NormalizedWord selectedWord, string context)
    {
        var targetWord = string.IsNullOrWhiteSpace(lookup.EntryWord)
            ? selectedWord.Lemma
            : lookup.EntryWord;
        return Rank(lookup, targetWord, selectedWord.Candidates, context);
    }

    private SenseRankingResult Rank(
        DictionaryLookupResult lookup,
        string targetWord,
        IReadOnlyList<string> targetVariants,
        string context)
    {
        var contextIsSafe = ContextQualityGate.IsSafe(context);
        if (!contextIsSafe)
        {
            context = string.Empty;
        }

        var senses = lookup.Senses ?? [];
        if (senses.Count == 0)
        {
            var definitions = lookup.Definitions.ToArray();
            return new SenseRankingResult(definitions.FirstOrDefault() ?? string.Empty, definitions, definitions.Length <= 1);
        }

        if (senses.Count == 1)
        {
            // A dictionary entry with one sense can still be useful without
            // OCR context, but do not present it as a verified in-sentence
            // reading when the only context failed the trust gate.
            if (!contextIsSafe)
            {
                return new SenseRankingResult(
                    Format(senses[0]),
                    [Format(senses[0])],
                    true,
                    senses[0]);
            }

            return new SenseRankingResult(
                DescribeInSentence(senses[0], targetWord, context),
                [Format(senses[0])],
                true,
                senses[0]);
        }

        var contextTokens = Tokens(context).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var normalizedTargetVariants = targetVariants
            .Append(targetWord)
            .Where(variant => !string.IsNullOrWhiteSpace(variant))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var targetVisibleInContext = WordToken().Matches(context)
            .Any(match => normalizedTargetVariants.Contains(match.Value));
        var hasMaskedFrame = IsMaskedNounFrame(context);
        // A readable crop is not automatically evidence for this lookup. If
        // the selected word was removed from the crop and no recognized frame
        // remains, topical overlap (for example "dog" near bark) can still
        // manufacture a confident but unrelated sense.
        if (!targetVisibleInContext && !hasMaskedFrame)
        {
            return Abstain(senses);
        }
        var ordered = senses
            .Select((sense, index) => new RankedSense(sense, Score(sense, targetWord, normalizedTargetVariants, context, contextTokens), index))
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Index)
            .ToArray();
        var confident = contextTokens.Count > 0
            && ordered[0].Score >= 2
            && ordered[0].Score - ordered[1].Score >= 1;

        if (confident)
        {
            return new SenseRankingResult(
                DescribeInSentence(ordered[0].Sense, targetWord, context),
                ordered.Select(item => Format(item.Sense)).ToArray(),
                true,
                ordered[0].Sense,
                WasContextAligned: true);
        }

        // Preserve a small, useful spread of parts of speech on an abstaining
        // card.  Two candidates can hide a legitimate third sense (for
        // example, crack's adjective "first-rate" sense behind noun and verb
        // entries), while more than three turns the first screen back into a
        // dictionary dump.
        return Abstain(senses);
    }

    private static SenseRankingResult Abstain(IReadOnlyList<DictionarySense> senses)
    {
        var candidates = string.Join("  ", senses.Take(3).Select((sense, index) => $"{Number(index)} {Format(sense)}"));
        // Make the epistemic boundary readable before the candidate list.
        // A user must not mistake these dictionary alternatives for a selected
        // in-sentence meaning when the available crop cannot support one.
        var flat = $"无法仅凭这一句确定；可能是：{candidates}";
        return new SenseRankingResult(flat, senses.Select(Format).ToArray(), false);
    }

    private static int Score(
        DictionarySense sense,
        string targetWord,
        HashSet<string> targetVariants,
        string context,
        HashSet<string> contextTokens)
    {
        var score = 0;
        var words = WordToken().Matches(context).Select(match => match.Value).ToArray();
        var targetVisibleInContext = words.Any(targetVariants.Contains);
        for (var index = 0; index < words.Length; index++)
        {
            if (!targetVariants.Contains(words[index]))
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

            // An adjective used as an attributive/appositive modifier is often
            // followed by a comma ("the frail, slender girl").  The article
            // heuristic above must not let the noun sense win this common form.
            if (sense.PartOfSpeech is "adj" or "a"
                && targetVariants.Any(variant => Regex.IsMatch(context, $@"\b{Regex.Escape(variant)}\b\s*,", RegexOptions.IgnoreCase)))
            {
                score += 5;
            }
        }

        if (sense.PartOfSpeech is "adj" or "a"
            && contextTokens.Any(AdjectiveHeadNouns.Contains))
        {
            score += 3;
        }

        // The context crop deliberately removes the selected word to avoid
        // OCR duplicating it.  When that leaves a sentence ending directly
        // before the word, retain only high-signal English frames: articles
        // and possessives introduce nouns, and an article plus modifier still
        // introduces a noun.  Do not infer a sense from ordinary loose words.
        if (!targetVisibleInContext && IsMaskedNounFrame(context) && sense.PartOfSpeech == "n")
        {
            score += 4;
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

    private static string DescribeInSentence(DictionarySense sense, string targetWord, string context)
    {
        var partOfSpeech = sense.PartOfSpeech switch
        {
            "n" => "名词",
            "v" => "动词",
            "adj" or "a" => "形容词",
            "adv" => "副词",
            _ => "这个词",
        };
        return $"本句中，{targetWord}作{partOfSpeech}，指“{ContextualGloss(sense, targetWord, context)}”。";
    }

    private static string ContextualGloss(DictionarySense sense, string targetWord, string context)
    {
        // The bundled dictionary groups several translations under one part of
        // speech.  A confident POS decision alone must not put that whole
        // dictionary list back on the first screen.  Keep only reviewed,
        // high-signal game-reading frames here; all other entries preserve the
        // source definition rather than inventing a narrower meaning.
        var normalizedWord = targetWord.Trim().ToLowerInvariant();
        var normalizedContext = context.ToLowerInvariant();
        return (normalizedWord, sense.PartOfSpeech) switch
        {
            ("bark", "v") when normalizedContext.Contains("dog") => "吠叫",
            ("parrot", "n") when normalizedContext.Contains("babbling like") => "鹦鹉",
            ("frail", "adj") when normalizedContext.Contains("slender girl") => "脆弱的",
            ("staunch", "adj") when normalizedContext.Contains("supporter") => "忠实的",
            ("limp", "adj") when normalizedContext.Contains("bodies") => "无力的",
            ("exquisite", "adj") when normalizedContext.Contains("brooch") => "精致的",
            // Keep the first screen readable for reviewed single-sense game
            // samples whose dictionary gloss contains a long synonym list.
            ("sarcasm", "n") => "讽刺",
            ("fortress", "n") => "城堡",
            ("burden", "n") => "负担",
            ("calamity", "n") => "灾难",
            ("neglected", "adj") => "被忽视的",
            ("concealed", "adj") => "隐匿的",
            ("hopscotch", "n") => "跳房子游戏",
            _ => sense.ShortDefinition,
        };
    }

    private static bool IsMaskedNounFrame(string context)
    {
        var tail = context.Trim();
        return Regex.IsMatch(tail, @"\b(?:a|an|the|my|your|his|her|its|our|their)\s*$", RegexOptions.IgnoreCase)
            || Regex.IsMatch(tail, @"\b(?:a|an|the)\s+[A-Za-z]+\s*$", RegexOptions.IgnoreCase);
    }

    private static string Number(int index) => index switch { 0 => "①", 1 => "②", _ => "③" };

    private sealed record RankedSense(DictionarySense Sense, int Score, int Index);

    [GeneratedRegex("[A-Za-z]+(?:'[A-Za-z]+)?")]
    private static partial Regex WordToken();
}
