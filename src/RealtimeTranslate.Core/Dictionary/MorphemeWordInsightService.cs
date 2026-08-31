using RealtimeTranslate.Core.Models;

namespace RealtimeTranslate.Core.Dictionary;

public sealed class MorphemeWordInsightService : IWordInsightService
{
    private readonly Dictionary<string, WordInsight> _entries = new(StringComparer.OrdinalIgnoreCase);

    public MorphemeWordInsightService(string wordCardsTsvPath)
    {
        if (!File.Exists(wordCardsTsvPath))
        {
            return;
        }
        foreach (var line in File.ReadLines(wordCardsTsvPath))
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
            {
                continue;
            }

            var fields = line.Split('\t');
            if (fields.Length < 4)
            {
                continue;
            }

            var word = fields[0].Trim();
            if (word.Length == 0)
            {
                continue;
            }

            // Four-column files combine ECDICT inflections, MorphyNet
            // relations, MorphoLex analyses, and locally curated hints.  The
            // row no longer says which field came from which source, so a
            // runtime label such as "MorphoLex (2018)" would be fabricated
            // provenance.  Fail closed: the dictionary can still provide a
            // separately attributed WordNet example, but this row must not
            // become a learning claim until the generator writes field-level
            // evidence.
            var parsed = 0d;
            var hasExplicitEvidence = fields.Length >= 6
                && !string.IsNullOrWhiteSpace(fields[4])
                && double.TryParse(fields[5], out parsed);
            var source = hasExplicitEvidence ? fields[4].Trim() : string.Empty;
            var confidence = hasExplicitEvidence ? Math.Clamp(parsed, 0, 1) : 0;
            var contextHint = hasExplicitEvidence ? fields[3].Trim() : string.Empty;

            _entries[word] = new WordInsight(
                fields[1].Trim(),
                fields[2].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries),
                contextHint,
                source,
                confidence);
        }
    }

    public WordInsight Lookup(string word)
    {
        return _entries.TryGetValue(word.Trim(), out var insight) ? insight : WordInsight.Empty;
    }
}
