using System.Text.Encodings.Web;
using System.Text.Json;

namespace RealtimeTranslate.Core.Stats;

public sealed class JsonWordQueryCounter : IWordQueryCounter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
    };

    private readonly string _path;
    private readonly Dictionary<string, int> _counts;

    public JsonWordQueryCounter(string path)
    {
        _path = path;
        _counts = Load(path);
    }

    public int Increment(string word)
    {
        var key = NormalizeKey(word);
        if (string.IsNullOrWhiteSpace(key))
        {
            return 0;
        }

        _counts.TryGetValue(key, out var count);
        count++;
        _counts[key] = count;
        Save();
        return count;
    }

    private static Dictionary<string, int> Load(string path)
    {
        if (!File.Exists(path))
        {
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }

        var json = File.ReadAllText(path);
        var data = JsonSerializer.Deserialize<Dictionary<string, int>>(json);
        return data is null
            ? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, int>(data, StringComparer.OrdinalIgnoreCase);
    }

    private void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, JsonSerializer.Serialize(_counts.OrderBy(item => item.Key).ToDictionary(), JsonOptions));
    }

    private static string NormalizeKey(string word)
    {
        return word.Trim().ToLowerInvariant();
    }
}
