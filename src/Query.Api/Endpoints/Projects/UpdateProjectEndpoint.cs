using FastEndpoints;
using Query.Core.Storage;

namespace Query.Api.Endpoints.Projects;

public class UpdateProjectRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateProjectEndpoint : Endpoint<UpdateProjectRequest>
{
    private readonly IProjectRepository _projects;

    public UpdateProjectEndpoint(IProjectRepository projects)
    {
        _projects = projects;
    }

    public override void Configure()
    {
        Put("/projects/{Id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(UpdateProjectRequest req, CancellationToken ct)
    {
        var existing = await _projects.GetByIdAsync(req.Id);
        if (existing is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        existing.Name = req.Name;
        existing.Description = req.Description;
        existing.UpdatedAt = DateTime.UtcNow;

        await _projects.UpdateAsync(existing);
        await Send.NoContentAsync(ct);
    }
}
