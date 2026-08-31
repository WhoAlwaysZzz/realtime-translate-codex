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

            var source = fields.Length >= 5 ? fields[4].Trim() : "MorphoLex (2018)";
            var confidence = fields.Length >= 6 && double.TryParse(fields[5], out var parsed)
                ? Math.Clamp(parsed, 0, 1)
                : (fields[1].Trim().Length > 0 || fields[3].Trim().Length > 0 ? 0.75 : 0);

            _entries[word] = new WordInsight(
                fields[1].Trim(),
                fields[2].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries),
                fields[3].Trim(),
                source,
                confidence);
        }
    }

    public WordInsight Lookup(string word)
    {
        return _entries.TryGetValue(word.Trim(), out var insight) ? insight : WordInsight.Empty;
    }
}
