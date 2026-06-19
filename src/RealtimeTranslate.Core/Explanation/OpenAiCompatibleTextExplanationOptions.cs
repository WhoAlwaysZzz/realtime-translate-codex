namespace RealtimeTranslate.Core.Explanation;

public sealed record OpenAiCompatibleTextExplanationOptions(
    string Provider,
    string BaseUrl,
    string Model,
    string? ApiKey)
{
    public static OpenAiCompatibleTextExplanationOptions DeepSeekDefault(string? apiKey = null)
    {
        return new OpenAiCompatibleTextExplanationOptions(
            "DeepSeek",
            "https://api.deepseek.com",
            "deepseek-v4-flash",
            apiKey);
    }
}
