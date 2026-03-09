using FastEndpoints;
using Query.Core.Storage;

namespace Query.Api.Endpoints.Projects;

public class DeleteProjectRequest
{
    public Guid Id { get; set; }
}

public class DeleteProjectEndpoint : Endpoint<DeleteProjectRequest>
{
    private readonly IProjectRepository _projects;

    public DeleteProjectEndpoint(IProjectRepository projects)
    {
        _projects = projects;
    }

    public override void Configure()
    {
        Delete("/projects/{Id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(DeleteProjectRequest req, CancellationToken ct)
    {
        var existing = await _projects.GetByIdAsync(req.Id);
        if (existing is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await _projects.SoftDeleteAsync(req.Id);
        await Send.NoContentAsync(ct);
    }
}
