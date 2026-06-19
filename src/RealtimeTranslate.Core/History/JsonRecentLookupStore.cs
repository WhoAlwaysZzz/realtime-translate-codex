using System.Text.Encodings.Web;
using System.Text.Json;
using RealtimeTranslate.Core.Models;

namespace RealtimeTranslate.Core.History;

public sealed class JsonRecentLookupStore : IRecentLookupStore
{
    private const int DefaultMaxEntries = 20;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
    };

    private readonly string _path;
    private readonly int _maxEntries;
    private readonly List<RecentLookupEntry> _entries;

    public JsonRecentLookupStore(string path, int maxEntries = DefaultMaxEntries)
    {
        _path = path;
        _maxEntries = Math.Max(1, maxEntries);
        _entries = Load(path)
            .OrderByDescending(entry => entry.OccurredAt)
            .Take(_maxEntries)
            .ToList();
    }

    public void Add(LookupPresentation presentation)
    {
        if (string.IsNullOrWhiteSpace(presentation.Title))
        {
            return;
        }

        _entries.Insert(0, new RecentLookupEntry(DateTimeOffset.Now, presentation));
        if (_entries.Count > _maxEntries)
        {
            _entries.RemoveRange(_maxEntries, _entries.Count - _maxEntries);
        }

        Save();
    }

    public IReadOnlyList<LookupPresentation> GetRecent(int count)
    {
        if (count <= 0)
        {
            return [];
        }

        return _entries
            .Take(count)
            .Select(entry => entry.Presentation)
            .ToArray();
    }

    private static IReadOnlyList<RecentLookupEntry> Load(string path)
    {
        if (!File.Exists(path))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<List<RecentLookupEntry>>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, JsonSerializer.Serialize(_entries, JsonOptions));
    }
}
