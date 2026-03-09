using System.Text.Json;
using FastEndpoints;
using Query.Core.Compilers;
using Query.Core.Domain;
using Query.Core.Schema;
using Query.Core.Storage;

namespace Query.Api.Endpoints.Conversations;

public class ConfirmSpecRequest
{
    public Guid ConversationId { get; set; }
}

public class ConfirmSpecResponse
{
    public string RawOutput { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public string Compiler { get; set; } = string.Empty;
    public string Dialect { get; set; } = string.Empty;
    public object? Spec { get; set; }
}

public class ConfirmSpecEndpoint : Endpoint<ConfirmSpecRequest, ConfirmSpecResponse>
{
    private readonly IConversationRepository _conversations;
    private readonly IProjectRepository _projects;
    private readonly CompilerRegistry _compilerRegistry;

    public ConfirmSpecEndpoint(
        IConversationRepository conversations,
        IProjectRepository projects,
        CompilerRegistry compilerRegistry)
    {
        _conversations = conversations;
        _projects = projects;
        _compilerRegistry = compilerRegistry;
    }

    public override void Configure()
    {
        Post("/conversations/{ConversationId}/confirm");
        AllowAnonymous();
    }

    public override async Task HandleAsync(ConfirmSpecRequest req, CancellationToken ct)
    {
        var conversation = await _conversations.GetByIdAsync(req.ConversationId);
        if (conversation is null || string.IsNullOrEmpty(conversation.CurrentSpecJson))
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var spec = JsonSerializer.Deserialize<QuerySpec>(conversation.CurrentSpecJson);
        if (spec is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var project = await _projects.GetByIdAsync(conversation.ProjectId);
        var schemaContext = project?.SchemaContextJson is not null
            ? JsonSerializer.Deserialize<SchemaContext>(project.SchemaContextJson) ?? new SchemaContext()
            : new SchemaContext();

        var permissionContext = new PermissionContext(conversation.UserId, schemaContext.PermissionRules);

        var compiler = _compilerRegistry.Resolve(spec.OutputFormat);
        var bundle = compiler.Compile(spec, permissionContext);

        // Save output bundle
        await _conversations.SaveOutputBundleAsync(new OutputBundleRecord
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            RawOutput = bundle.RawOutput,
            Explanation = bundle.Explanation,
            SpecJson = JsonSerializer.Serialize(spec),
            Compiler = bundle.Compiler,
            Dialect = bundle.Dialect,
            CreatedAt = DateTime.UtcNow
        });

        // Update state to Done
        await _conversations.UpdateStateAsync(conversation.Id, "Done");

        await Send.OkAsync(new ConfirmSpecResponse
        {
            RawOutput = bundle.RawOutput,
            Explanation = bundle.Explanation,
            Compiler = bundle.Compiler,
            Dialect = bundle.Dialect,
            Spec = spec
        }, ct);
    }
}
