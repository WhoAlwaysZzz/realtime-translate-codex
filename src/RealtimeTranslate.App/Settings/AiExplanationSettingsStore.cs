using System.IO;
using System.Text.Json;
using RealtimeTranslate.Core.Explanation;

namespace RealtimeTranslate.App.Settings;

public sealed class AiExplanationSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private readonly string _path;

    public AiExplanationSettingsStore(string path)
    {
        _path = path;
    }

    public OpenAiCompatibleTextExplanationOptions Load()
    {
        var fallback = OpenAiCompatibleTextExplanationOptions.DeepSeekDefault(GetEnvironmentApiKey());
        if (!File.Exists(_path))
        {
            return fallback;
        }

        try
        {
            var json = File.ReadAllText(_path);
            var dto = JsonSerializer.Deserialize<AiExplanationSettingsDto>(json, JsonOptions);
            if (dto is null)
            {
                return fallback;
            }

            return new OpenAiCompatibleTextExplanationOptions(
                string.IsNullOrWhiteSpace(dto.Provider) ? fallback.Provider : dto.Provider,
                string.IsNullOrWhiteSpace(dto.BaseUrl) ? fallback.BaseUrl : dto.BaseUrl,
                string.IsNullOrWhiteSpace(dto.Model) ? fallback.Model : dto.Model,
                FirstNonBlank(GetEnvironmentApiKey(), dto.ApiKey));
        }
        catch
        {
            return fallback;
        }
    }

    public void EnsureTemplateExists()
    {
        if (File.Exists(_path))
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var dto = new AiExplanationSettingsDto(
            "DeepSeek",
            "https://api.deepseek.com",
            "deepseek-v4-flash",
            "");
        File.WriteAllText(_path, JsonSerializer.Serialize(dto, JsonOptions));
    }

    private static string? GetEnvironmentApiKey()
    {
        return Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");
    }

    private static string? FirstNonBlank(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }

    private sealed record AiExplanationSettingsDto(
        string? Provider,
        string? BaseUrl,
        string? Model,
        string? ApiKey);
}
