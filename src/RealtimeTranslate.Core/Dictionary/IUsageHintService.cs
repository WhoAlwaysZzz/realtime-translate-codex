using RealtimeTranslate.Core.Models;

namespace RealtimeTranslate.Core.Dictionary;

public interface IUsageHintService
{
    UsageHintResult GetHint(DictionaryLookupResult lookup, SenseRankingResult ranking, string targetWord);
}
