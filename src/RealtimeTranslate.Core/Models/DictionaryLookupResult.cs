namespace RealtimeTranslate.Core.Models;

public sealed record DictionaryLookupResult(
    bool Found,
    string EntryWord,
    IReadOnlyList<string> Definitions,
    IReadOnlyList<DictionarySense>? Senses = null,
    int FrequencyBand = 0)
{
    public static DictionaryLookupResult NotFound(string word) => new(false, word, []);
}
