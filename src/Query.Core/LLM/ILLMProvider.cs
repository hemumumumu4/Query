namespace Query.Core.LLM;

public interface ILLMProvider
{
    Task<string> CompleteAsync(string systemPrompt, string userPrompt, LLMOptions options);
}
