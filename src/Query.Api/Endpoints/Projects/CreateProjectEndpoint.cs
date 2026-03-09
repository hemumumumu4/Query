using FastEndpoints;
using Query.Core.Storage;

namespace Query.Api.Endpoints.Projects;

public class CreateProjectRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class CreateProjectResponse
{
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class CreateProjectEndpoint : Endpoint<CreateProjectRequest, CreateProjectResponse>
{
    private readonly IProjectRepository _projects;

    public CreateProjectEndpoint(IProjectRepository projects)
    {
        _projects = projects;
    }

    public override void Configure()
    {
        Post("/projects");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateProjectRequest req, CancellationToken ct)
    {
        var record = new ProjectRecord
        {
            Id = Guid.NewGuid(),
            Name = req.Name,
            Description = req.Description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _projects.CreateAsync(record);

        await Send.CreatedAtAsync<GetProjectEndpoint>(
            new { id = record.Id },
            new CreateProjectResponse { ProjectId = record.Id, Name = record.Name },
            cancellation: ct);
    }
}
