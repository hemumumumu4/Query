using System.Text.Json;
using FastEndpoints;
using Query.Core.Ingestion;
using Query.Core.Storage;

namespace Query.Api.Endpoints.Projects;

public class CreateProjectRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DdlContent { get; set; } = string.Empty;
    public string? MarkdownContent { get; set; }
    public string? PermissionsYaml { get; set; }
    public string? CalculationsYaml { get; set; }
}

public class CreateProjectResponse
{
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class CreateProjectEndpoint : Endpoint<CreateProjectRequest, CreateProjectResponse>
{
    private readonly IProjectRepository _projects;
    private readonly SchemaContextBuilder _builder;

    public CreateProjectEndpoint(IProjectRepository projects, SchemaContextBuilder builder)
    {
        _projects = projects;
        _builder = builder;
    }

    public override void Configure()
    {
        Post("/projects");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateProjectRequest req, CancellationToken ct)
    {
        _builder
            .Register("ddl", new DDLAdapter())
            .Register("markdown", new MarkdownAdapter())
            .Register("permissions", new PermissionAdapter())
            .Add("ddl", req.DdlContent);

        if (!string.IsNullOrWhiteSpace(req.MarkdownContent))
            _builder.Add("markdown", req.MarkdownContent);

        if (!string.IsNullOrWhiteSpace(req.PermissionsYaml))
            _builder.Add("permissions", req.PermissionsYaml);

        var schemaContext = await _builder.BuildAsync();
        var schemaJson = JsonSerializer.Serialize(schemaContext);

        var record = new ProjectRecord
        {
            Id = Guid.NewGuid(),
            Name = req.Name,
            Description = req.Description,
            SchemaContextJson = schemaJson,
            CreatedAt = DateTime.UtcNow
        };

        await _projects.CreateAsync(record);

        await Send.CreatedAtAsync<GetProjectEndpoint>(
            new { id = record.Id },
            new CreateProjectResponse { ProjectId = record.Id, Name = record.Name },
            cancellation: ct);
    }
}
