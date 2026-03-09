namespace Query.Core.LLM;

public record LLMOptions(float Temperature, string ResponseFormat, int MaxTokens);
