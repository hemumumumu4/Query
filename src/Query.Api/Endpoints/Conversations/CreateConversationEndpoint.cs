using System.Security.Claims;
using FastEndpoints;
using Query.Core.Storage;

namespace Query.Api.Endpoints.Conversations;

public class CreateConversationRequest
{
    public Guid ProjectId { get; set; }
}

public class CreateConversationResponse
{
    public Guid ConversationId { get; set; }
}

public class CreateConversationEndpoint : Endpoint<CreateConversationRequest, CreateConversationResponse>
{
    private readonly IConversationRepository _conversations;
    private readonly IProjectRepository _projects;

    public CreateConversationEndpoint(IConversationRepository conversations, IProjectRepository projects)
    {
        _conversations = conversations;
        _projects = projects;
    }

    public override void Configure()
    {
        Post("/projects/{ProjectId}/conversations");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateConversationRequest req, CancellationToken ct)
    {
        var project = await _projects.GetByIdAsync(req.ProjectId);
        if (project is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var userId = ExtractUserId();

        var record = new ConversationRecord
        {
            Id = Guid.NewGuid(),
            ProjectId = req.ProjectId,
            UserId = userId,
            State = "SchemaLoaded",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _conversations.CreateAsync(record);

        await Send.OkAsync(new CreateConversationResponse
        {
            ConversationId = record.Id
        }, ct);
    }

    private string ExtractUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub")
                  ?? User.FindFirstValue("user_id");

        return sub ?? "anonymous";
    }
}
