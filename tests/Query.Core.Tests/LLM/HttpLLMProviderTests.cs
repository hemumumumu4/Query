using FluentAssertions;
using Moq;
using Query.Core.LLM;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Query.Core.Tests.LLM;

public class HttpLLMProviderTests
{
    [Fact]
    public async Task CompleteAsync_ReturnsContentFromResponse()
    {
        var response = JsonSerializer.Serialize(new
        {
            choices = new[] { new { message = new { content = "test response" } } }
        });

        var handler = new MockHttpMessageHandler(response, HttpStatusCode.OK);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.test.com") };
        var config = new LLMProviderConfig("https://api.test.com", "Bearer", "test-key", "gpt-4");
        var provider = new HttpLLMProvider(httpClient, config);

        var result = await provider.CompleteAsync("system", "user", new LLMOptions(0f, "json", 1000));

        result.Should().Be("test response");
    }

    [Fact]
    public async Task CompleteAsync_SendsCorrectAuthHeader()
    {
        var handler = new MockHttpMessageHandler(
            JsonSerializer.Serialize(new { choices = new[] { new { message = new { content = "ok" } } } }),
            HttpStatusCode.OK);

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.test.com") };
        var config = new LLMProviderConfig("https://api.test.com", "Bearer", "my-secret", "gpt-4");
        var provider = new HttpLLMProvider(httpClient, config);

        await provider.CompleteAsync("sys", "user", new LLMOptions(0f, "json", 100));

        handler.LastRequest!.Headers.Authorization!.Scheme.Should().Be("Bearer");
        handler.LastRequest.Headers.Authorization.Parameter.Should().Be("my-secret");
    }
}

public class MockHttpMessageHandler(string responseContent, HttpStatusCode statusCode) : HttpMessageHandler
{
    public HttpRequestMessage? LastRequest { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        LastRequest = request;
        return Task.FromResult(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
        });
    }
}
