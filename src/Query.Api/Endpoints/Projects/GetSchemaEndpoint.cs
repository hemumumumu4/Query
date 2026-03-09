using System.Text.Json;
using FastEndpoints;
using Query.Core.Storage;

namespace Query.Api.Endpoints.Projects;

public class GetSchemaRequest
{
    public Guid Id { get; set; }
}

public class GetSchemaEndpoint : Endpoint<GetSchemaRequest>
{
    private readonly IProjectRepository _projects;

    public GetSchemaEndpoint(IProjectRepository projects)
    {
        _projects = projects;
    }

    public override void Configure()
    {
        Get("/projects/{Id}/schema");
        AllowAnonymous();
    }

    public override async Task HandleAsync(GetSchemaRequest req, CancellationToken ct)
    {
        var project = await _projects.GetByIdAsync(req.Id);
        if (project is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (string.IsNullOrEmpty(project.SchemaContextJson))
        {
            await Send.OkAsync(new { }, ct);
            return;
        }

        var schema = JsonSerializer.Deserialize<JsonElement>(project.SchemaContextJson);
        await Send.OkAsync(schema, ct);
    }
}
