using FastEndpoints;
using Query.Core.Storage;

namespace Query.Api.Endpoints.Projects;

public class ListProjectsResponse
{
    public List<ListProjectItem> Projects { get; set; } = [];
}

public class ListProjectItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ListProjectsEndpoint : EndpointWithoutRequest<ListProjectsResponse>
{
    private readonly IProjectRepository _projects;

    public ListProjectsEndpoint(IProjectRepository projects)
    {
        _projects = projects;
    }

    public override void Configure()
    {
        Get("/projects");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var records = await _projects.ListAsync();

        await Send.OkAsync(new ListProjectsResponse
        {
            Projects = records.Select(r => new ListProjectItem
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            }).ToList()
        }, ct);
    }
}
