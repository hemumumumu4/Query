using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Query.Core.LLM;

public class HttpLLMProvider(HttpClient httpClient, LLMProviderConfig config) : ILLMProvider
{
    public async Task<string> CompleteAsync(string systemPrompt, string userPrompt, LLMOptions options)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue(config.AuthScheme, config.AuthToken);

        var body = new
        {
            model = config.Model,
            temperature = options.Temperature,
            max_tokens = options.MaxTokens,
            response_format = options.ResponseFormat == "json" ? new { type = "json_object" } : null,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            }
        };

        request.Content = new StringContent(
            JsonSerializer.Serialize(body, new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            }),
            Encoding.UTF8,
            "application/json");

        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;
    }
}
