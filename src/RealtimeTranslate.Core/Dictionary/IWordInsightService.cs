using RealtimeTranslate.Core.Models;

namespace RealtimeTranslate.Core.Dictionary;

public interface IWordInsightService
{
    WordInsight Lookup(string word);
}
