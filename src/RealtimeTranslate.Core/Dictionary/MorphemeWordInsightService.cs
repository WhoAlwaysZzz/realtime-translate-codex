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
            if (fields.Length != 4)
            {
                continue;
            }

            var word = fields[0].Trim();
            if (word.Length == 0)
            {
                continue;
            }

            _entries[word] = new WordInsight(
                fields[1].Trim(),
                fields[2].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries),
                fields[3].Trim());
        }
    }

    public WordInsight Lookup(string word)
    {
        return _entries.TryGetValue(word.Trim(), out var insight) ? insight : WordInsight.Empty;
    }
}
