using RealtimeTranslate.Core.Models;

namespace RealtimeTranslate.Core.Dictionary;

public interface ISenseRanker
{
    SenseRankingResult Rank(DictionaryLookupResult lookup, string targetWord, string context);
}
