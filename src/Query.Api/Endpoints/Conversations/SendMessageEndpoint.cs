using System.Text.Json;
using FastEndpoints;
using Query.Core.Conversation;
using Query.Core.LLM;
using Query.Core.Schema;
using Query.Core.Storage;

namespace Query.Api.Endpoints.Conversations;

public class SendMessageRequest
{
    public Guid ConversationId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class SendMessageResponse
{
    public string AssistantMessage { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public object? PartialSpec { get; set; }
}

public class SendMessageEndpoint : Endpoint<SendMessageRequest, SendMessageResponse>
{
    private readonly IConversationRepository _conversations;
    private readonly IProjectRepository _projects;
    private readonly ILLMProvider _llm;

    public SendMessageEndpoint(IConversationRepository conversations, IProjectRepository projects, ILLMProvider llm)
    {
        _conversations = conversations;
        _projects = projects;
        _llm = llm;
    }

    public override void Configure()
    {
        Post("/conversations/{ConversationId}/messages");
        AllowAnonymous();
    }

    public override async Task HandleAsync(SendMessageRequest req, CancellationToken ct)
    {
        var conversation = await _conversations.GetByIdAsync(req.ConversationId);
        if (conversation is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var project = await _projects.GetByIdAsync(conversation.ProjectId);
        if (project is null || string.IsNullOrEmpty(project.SchemaContextJson))
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var schemaContext = JsonSerializer.Deserialize<SchemaContext>(project.SchemaContextJson)
                           ?? new SchemaContext();

        var permissionContext = new PermissionContext(conversation.UserId, schemaContext.PermissionRules);

        var session = new ConversationSession(_llm, schemaContext, permissionContext);

        // Replay existing history
        var turns = await _conversations.GetTurnsAsync(conversation.Id);
        foreach (var turn in turns)
        {
            session.History.Add(new ConversationTurn(turn.Role, turn.Content));
        }

        // Send the new message
        var response = await session.SendMessageAsync(req.Message);

        // Save user turn
        await _conversations.AddTurnAsync(new ConversationTurnRecord
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Role = "user",
            Content = req.Message,
            CreatedAt = DateTime.UtcNow
        });

        // Save assistant turn
        await _conversations.AddTurnAsync(new ConversationTurnRecord
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Role = "assistant",
            Content = response.Message,
            CreatedAt = DateTime.UtcNow
        });

        // Update conversation state and spec
        var specJson = response.Spec is not null ? JsonSerializer.Serialize(response.Spec) : conversation.CurrentSpecJson;
        await _conversations.UpdateStateAndSpecAsync(conversation.Id, session.State.ToString(), specJson);

        await Send.OkAsync(new SendMessageResponse
        {
            AssistantMessage = response.Message,
            State = session.State.ToString(),
            PartialSpec = response.Spec
        }, ct);
    }
}
