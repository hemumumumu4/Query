using System.Text.Json;
using System.Text.Json.Serialization;
using Query.Core.Domain;
using Query.Core.LLM;
using Query.Core.Schema;

namespace Query.Core.Conversation;

public record ConversationTurn(string Role, string Content);

public record ConversationResponse(string Message, QuerySpec? Spec = null);

public class ConversationSession
{
    private readonly ILLMProvider _llm;
    private readonly SchemaContext _schema;
    private readonly PermissionContext _permissions;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ConversationState State { get; private set; } = ConversationState.SchemaLoaded;
    public QuerySpec? CurrentSpec { get; private set; }
    public List<ConversationTurn> History { get; } = [];

    public ConversationSession(ILLMProvider llm, SchemaContext schema, PermissionContext permissions)
    {
        _llm = llm;
        _schema = schema;
        _permissions = permissions;
    }

    public async Task<ConversationResponse> SendMessageAsync(string userMessage)
    {
        History.Add(new ConversationTurn("user", userMessage));

        var schemaJson = JsonSerializer.Serialize(_schema.Tables, JsonOptions);
        var systemPrompt = PromptTemplates.IntentExtraction(schemaJson);
        var options = new LLMOptions(Temperature: 0f, ResponseFormat: "json", MaxTokens: 2048);

        var llmResponse = await _llm.CompleteAsync(systemPrompt, userMessage, options);
        var extraction = JsonSerializer.Deserialize<IntentExtractionResult>(llmResponse, JsonOptions);

        if (extraction is null)
        {
            State = ConversationState.Disambiguation;
            var errorResponse = new ConversationResponse("I could not understand your request. Could you rephrase?");
            History.Add(new ConversationTurn("assistant", errorResponse.Message));
            return errorResponse;
        }

        if (extraction.ClarificationNeeded)
        {
            State = ConversationState.Disambiguation;
            var clarifyResponse = new ConversationResponse(extraction.ClarificationQuestion ?? "Could you provide more details?");
            History.Add(new ConversationTurn("assistant", clarifyResponse.Message));
            return clarifyResponse;
        }

        var spec = BuildQuerySpec(extraction);
        CurrentSpec = spec;
        State = ConversationState.SpecConfirmed;

        var summary = new QuerySpecSummary(
            extraction.Intent ?? "unknown",
            extraction.Entities?.Select(e => $"{e.Table} ({e.Alias})").ToList() ?? [],
            extraction.Measures?.Select(m => $"{m.Expression} as {m.Alias}").ToList() ?? [],
            extraction.Dimensions?.Select(d => $"{d.Expression} as {d.Alias}").ToList() ?? [],
            extraction.Filters?.Select(f => $"{f.Expression} {f.Operator} {f.Value}").ToList() ?? []);

        var confirmMessage = PromptTemplates.SpecConfirmation(summary);
        History.Add(new ConversationTurn("assistant", confirmMessage));
        return new ConversationResponse(confirmMessage, spec);
    }

    private QuerySpec BuildQuerySpec(IntentExtractionResult extraction) => new()
    {
        Intent = extraction.Intent ?? "unknown",
        Entities = extraction.Entities?
            .Select(e => new EntityRef(e.Table ?? "", e.Alias ?? ""))
            .ToList() ?? [],
        Measures = extraction.Measures?
            .Select(m => new MeasureDef(m.Expression ?? "", m.Alias ?? ""))
            .ToList() ?? [],
        Dimensions = extraction.Dimensions?
            .Select(d => new DimensionDef(d.Expression ?? "", d.Alias ?? ""))
            .ToList() ?? [],
        Filters = extraction.Filters?
            .Select(f => new FilterDef(f.Expression ?? "", f.Operator ?? "", f.Value ?? ""))
            .ToList() ?? []
    };

    // Internal DTOs for LLM JSON deserialization
    private sealed class IntentExtractionResult
    {
        [JsonPropertyName("intent")]
        public string? Intent { get; set; }

        [JsonPropertyName("entities")]
        public List<EntityDto>? Entities { get; set; }

        [JsonPropertyName("measures")]
        public List<MeasureDto>? Measures { get; set; }

        [JsonPropertyName("dimensions")]
        public List<DimensionDto>? Dimensions { get; set; }

        [JsonPropertyName("filters")]
        public List<FilterDto>? Filters { get; set; }

        [JsonPropertyName("clarification_needed")]
        public bool ClarificationNeeded { get; set; }

        [JsonPropertyName("clarification_question")]
        public string? ClarificationQuestion { get; set; }
    }

    private sealed class EntityDto
    {
        [JsonPropertyName("table")]
        public string? Table { get; set; }

        [JsonPropertyName("alias")]
        public string? Alias { get; set; }
    }

    private sealed class MeasureDto
    {
        [JsonPropertyName("expression")]
        public string? Expression { get; set; }

        [JsonPropertyName("alias")]
        public string? Alias { get; set; }
    }

    private sealed class DimensionDto
    {
        [JsonPropertyName("expression")]
        public string? Expression { get; set; }

        [JsonPropertyName("alias")]
        public string? Alias { get; set; }
    }

    private sealed class FilterDto
    {
        [JsonPropertyName("expression")]
        public string? Expression { get; set; }

        [JsonPropertyName("operator")]
        public string? Operator { get; set; }

        [JsonPropertyName("value")]
        public string? Value { get; set; }
    }
}
