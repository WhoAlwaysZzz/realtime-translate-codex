using System.IO.Compression;
using System.Text.Json;
using RealtimeTranslate.Core.Models;

namespace RealtimeTranslate.Core.Dictionary;

public sealed class StructuredDictionaryService : IDictionaryService
{
    private readonly Dictionary<string, LexiconEntry> _entries = new(StringComparer.OrdinalIgnoreCase);
    private readonly IDictionaryService _fallback;

    public StructuredDictionaryService(string gzipJsonLinesPath, IDictionaryService fallback)
    {
        _fallback = fallback;
        if (!File.Exists(gzipJsonLinesPath))
        {
            return;
        }

        using var file = File.OpenRead(gzipJsonLinesPath);
        using var gzip = new GZipStream(file, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip);
        while (reader.ReadLine() is { } line)
        {
            var entry = JsonSerializer.Deserialize<LexiconEntry>(line);
            if (entry is not null && !string.IsNullOrWhiteSpace(entry.Word) && entry.Senses.Count > 0)
            {
                _entries[entry.Word] = entry;
            }
        }
    }

    public DictionaryLookupResult Lookup(NormalizedWord word)
    {
        foreach (var candidate in word.Candidates.Where(candidate => !string.IsNullOrWhiteSpace(candidate)))
        {
            if (!_entries.TryGetValue(candidate, out var entry))
            {
                continue;
            }

            var senses = entry.Senses
                .Select(sense => new DictionarySense(sense.PartOfSpeech, sense.ShortDefinition, sense.EnglishDefinitions, sense.EnglishExamples))
                .ToArray();
            return new DictionaryLookupResult(
                true,
                candidate,
                senses.Select(sense => FormatDefinition(sense)).ToArray(),
                senses,
                entry.FrequencyBand);
        }

        return _fallback.Lookup(word);
    }

    private static string FormatDefinition(DictionarySense sense)
    {
        return string.IsNullOrWhiteSpace(sense.PartOfSpeech)
            ? sense.ShortDefinition
            : $"{sense.PartOfSpeech}. {sense.ShortDefinition}";
    }

    private sealed record LexiconEntry(
        [property: System.Text.Json.Serialization.JsonPropertyName("w")] string Word,
        [property: System.Text.Json.Serialization.JsonPropertyName("f")] int FrequencyBand,
        [property: System.Text.Json.Serialization.JsonPropertyName("s")] IReadOnlyList<LexiconSense> Senses);

    private sealed record LexiconSense(
        [property: System.Text.Json.Serialization.JsonPropertyName("p")] string PartOfSpeech,
        [property: System.Text.Json.Serialization.JsonPropertyName("z")] string ShortDefinition,
        [property: System.Text.Json.Serialization.JsonPropertyName("d")] IReadOnlyList<string> EnglishDefinitions,
        [property: System.Text.Json.Serialization.JsonPropertyName("x")] IReadOnlyList<string> EnglishExamples);
}
