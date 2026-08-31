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

            var hasExplicitSource = fields.Length >= 5 && !string.IsNullOrWhiteSpace(fields[4]);
            var source = hasExplicitSource ? fields[4].Trim() : "MorphoLex (2018)";
            var confidence = fields.Length >= 6 && double.TryParse(fields[5], out var parsed)
                ? Math.Clamp(parsed, 0, 1)
                : (fields[1].Trim().Length > 0 || fields[2].Trim().Length > 0 || fields[3].Trim().Length > 0 ? 0.75 : 0);
            // The legacy four-column export has no provenance for its
            // scenario-specific context hint. MorphoLex supports morphology,
            // not invented game scenes; omit that weak field unless a source
            // is explicitly attached to the row.
            var contextHint = hasExplicitSource ? fields[3].Trim() : string.Empty;

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
