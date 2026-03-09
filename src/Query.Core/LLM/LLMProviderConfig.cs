namespace Query.Core.LLM;

public record LLMProviderConfig(string BaseUrl, string AuthScheme, string AuthToken, string Model);
