using System.Text.Json;
using System.Text.Json.Nodes;
using FastEndpoints;
using Query.Core.Storage;

namespace Query.Api.Endpoints.Projects;

public class PatchSchemaRequest
{
    public Guid Id { get; set; }
    public JsonElement Body { get; set; }
}

public class PatchSchemaEndpoint : Endpoint<PatchSchemaRequest>
{
    private readonly IProjectRepository _projects;

    public PatchSchemaEndpoint(IProjectRepository projects)
    {
        _projects = projects;
    }

    public override void Configure()
    {
        Patch("/projects/{Id}/schema");
        AllowAnonymous();
    }

    public override async Task HandleAsync(PatchSchemaRequest req, CancellationToken ct)
    {
        var project = await _projects.GetByIdAsync(req.Id);
        if (project is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var existing = string.IsNullOrEmpty(project.SchemaContextJson)
            ? new JsonObject()
            : JsonNode.Parse(project.SchemaContextJson)!.AsObject();

        var patch = JsonNode.Parse(req.Body.GetRawText())!.AsObject();

        foreach (var prop in patch)
        {
            existing[prop.Key] = prop.Value?.DeepClone();
        }

        var merged = existing.ToJsonString();
        await _projects.UpdateSchemaContextAsync(req.Id, merged);
        await Send.NoContentAsync(ct);
    }
}
