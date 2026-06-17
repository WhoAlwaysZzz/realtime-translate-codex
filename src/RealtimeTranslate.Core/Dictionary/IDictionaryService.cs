using RealtimeTranslate.Core.Models;

namespace RealtimeTranslate.Core.Dictionary;

public interface IDictionaryService
{
    DictionaryLookupResult Lookup(NormalizedWord word);
}
