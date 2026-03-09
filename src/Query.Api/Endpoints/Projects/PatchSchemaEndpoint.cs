using System.Text.Json.Nodes;
using FastEndpoints;
using Query.Core.Storage;

namespace Query.Api.Endpoints.Projects;

public class PatchSchemaEndpoint : EndpointWithoutRequest
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

    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<Guid>("Id");

        var project = await _projects.GetByIdAsync(id);
        if (project is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        using var reader = new StreamReader(HttpContext.Request.Body);
        var body = await reader.ReadToEndAsync(ct);

        var existing = string.IsNullOrEmpty(project.SchemaContextJson)
            ? new JsonObject()
            : JsonNode.Parse(project.SchemaContextJson)!.AsObject();

        var patch = JsonNode.Parse(body)!.AsObject();

        foreach (var prop in patch)
        {
            existing[prop.Key] = prop.Value?.DeepClone();
        }

        var merged = existing.ToJsonString();
        await _projects.UpdateSchemaContextAsync(id, merged);
        await Send.NoContentAsync(ct);
    }
}
