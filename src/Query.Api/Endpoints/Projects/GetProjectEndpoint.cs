using FastEndpoints;
using Query.Core.Storage;

namespace Query.Api.Endpoints.Projects;

public class GetProjectRequest
{
    public Guid Id { get; set; }
}

public class GetProjectResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? SchemaContextJson { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class GetProjectEndpoint : Endpoint<GetProjectRequest, GetProjectResponse>
{
    private readonly IProjectRepository _projects;

    public GetProjectEndpoint(IProjectRepository projects)
    {
        _projects = projects;
    }

    public override void Configure()
    {
        Get("/projects/{Id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(GetProjectRequest req, CancellationToken ct)
    {
        var project = await _projects.GetByIdAsync(req.Id);

        if (project is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(new GetProjectResponse
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            SchemaContextJson = project.SchemaContextJson,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt
        }, ct);
    }
}
