using System.Text.Encodings.Web;
using System.Text.Json;
using RealtimeTranslate.Core.Models;

namespace RealtimeTranslate.Core.Recording;

public sealed class JsonlLookupRecorder : ILookupRecorder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _recordsDirectory;
    private readonly string _jsonlPath;

    public JsonlLookupRecorder(string archiveRoot)
    {
        _recordsDirectory = Path.Combine(archiveRoot, "records");
        _jsonlPath = Path.Combine(_recordsDirectory, "lookups.jsonl");
    }

    public void Record(LookupRecord record)
    {
        Directory.CreateDirectory(_recordsDirectory);
        var line = JsonSerializer.Serialize(record, JsonOptions);
        File.AppendAllText(_jsonlPath, line + Environment.NewLine);
    }
}
