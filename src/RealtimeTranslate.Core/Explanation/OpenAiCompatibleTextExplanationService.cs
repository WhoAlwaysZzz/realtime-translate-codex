using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using RealtimeTranslate.Core.Models;

namespace RealtimeTranslate.Core.Explanation;

public sealed class OpenAiCompatibleTextExplanationService : ITextExplanationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly HttpClient _httpClient;
    private readonly OpenAiCompatibleTextExplanationOptions _options;

    public OpenAiCompatibleTextExplanationService(HttpClient httpClient, OpenAiCompatibleTextExplanationOptions options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<TextExplanation> ExplainAsync(string sourceText, CancellationToken cancellationToken = default)
    {
        var trimmed = sourceText.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new InvalidOperationException("No text was recognized for explanation.");
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("AI explanation is not configured. Set DEEPSEEK_API_KEY or configure ai-settings.json.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, BuildChatCompletionsUri());
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        request.Content = JsonContent.Create(CreateRequestBody(trimmed), options: JsonOptions);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"AI explanation request failed: {(int)response.StatusCode} {response.ReasonPhrase}. {TrimForDisplay(responseBody)}");
        }

        var completion = JsonSerializer.Deserialize<ChatCompletionResponse>(responseBody, JsonOptions);
        var content = completion?.Choices.FirstOrDefault()?.Message.Content;
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("AI explanation response did not contain text.");
        }

        var payload = ParseExplanationPayload(content);
        return new TextExplanation(trimmed, payload.Translation, payload.Notes, _options.Provider, _options.Model);
    }

    private Uri BuildChatCompletionsUri()
    {
        return new Uri($"{_options.BaseUrl.TrimEnd('/')}/chat/completions");
    }

    private object CreateRequestBody(string sourceText)
    {
        return new
        {
            model = _options.Model,
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = "You translate and explain English text for a Chinese learner. Return strict JSON only.",
                },
                new
                {
                    role = "user",
                    content = $$"""
                    Explain this English text in Chinese.

                    Return JSON with this exact shape:
                    {
                      "translation": "自然、简洁的中文翻译",
                      "notes": ["最多 5 条关键表达、难句结构或语境说明"]
                    }

                    English text:
                    {{sourceText}}
                    """,
                },
            },
            response_format = new
            {
                type = "json_object",
            },
            temperature = 0.2,
            max_tokens = 1200,
            stream = false,
        };
    }

    private static ExplanationPayload ParseExplanationPayload(string content)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<ExplanationPayload>(content, JsonOptions);
            if (payload is not null && !string.IsNullOrWhiteSpace(payload.Translation))
            {
                return payload with
                {
                    Notes = payload.Notes.Where(note => !string.IsNullOrWhiteSpace(note)).Take(5).ToArray(),
                };
            }
        }
        catch (JsonException)
        {
        }

        return new ExplanationPayload(content.Trim(), []);
    }

    private static string TrimForDisplay(string text)
    {
        var trimmed = text.Trim();
        return trimmed.Length <= 240 ? trimmed : trimmed[..240];
    }

    private sealed record ChatCompletionResponse(IReadOnlyList<ChatChoice> Choices);

    private sealed record ChatChoice(ChatMessage Message);

    private sealed record ChatMessage(string Content);

    private sealed record ExplanationPayload(
        [property: JsonPropertyName("translation")] string Translation,
        [property: JsonPropertyName("notes")] IReadOnlyList<string> Notes);
}
