using FastEndpoints;
using Query.Core.Storage;

namespace Query.Api.Endpoints.Conversations;

public class GetSpecRequest
{
    public Guid ConversationId { get; set; }
}

public class GetSpecResponse
{
    public string? SpecJson { get; set; }
    public string State { get; set; } = string.Empty;
}

public class GetSpecEndpoint : Endpoint<GetSpecRequest, GetSpecResponse>
{
    private readonly IConversationRepository _conversations;

    public GetSpecEndpoint(IConversationRepository conversations)
    {
        _conversations = conversations;
    }

    public override void Configure()
    {
        Get("/conversations/{ConversationId}/spec");
        AllowAnonymous();
    }

    public override async Task HandleAsync(GetSpecRequest req, CancellationToken ct)
    {
        var conversation = await _conversations.GetByIdAsync(req.ConversationId);
        if (conversation is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(new GetSpecResponse
        {
            SpecJson = conversation.CurrentSpecJson,
            State = conversation.State
        }, ct);
    }
}
