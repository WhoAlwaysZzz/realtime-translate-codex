namespace RealtimeTranslate.Core.Models;

public sealed record DictionaryLookupResult(
    bool Found,
    string EntryWord,
    IReadOnlyList<string> Definitions)
{
    public static DictionaryLookupResult NotFound(string word) => new(false, word, []);
}
