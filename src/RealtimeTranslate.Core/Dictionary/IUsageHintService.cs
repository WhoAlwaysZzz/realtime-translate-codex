using RealtimeTranslate.Core.Models;

namespace RealtimeTranslate.Core.Dictionary;

public interface IUsageHintService
{
    string GetHint(DictionaryLookupResult lookup, SenseRankingResult ranking, string targetWord);
}
