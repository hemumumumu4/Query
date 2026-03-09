using System.Text.Json;
using FastEndpoints;
using Query.Core.Schema;
using Query.Core.Storage;

namespace Query.Api.Endpoints.Projects;

public class PutSchemaRequest
{
    public Guid Id { get; set; }
    public List<TableDef> Tables { get; set; } = [];
    public List<RelationshipDef> Relationships { get; set; } = [];
    public Dictionary<string, string> BusinessTerms { get; set; } = [];
    public Dictionary<string, CalculationEntry> CalculationLibrary { get; set; } = [];
    public Dictionary<string, string> Glossary { get; set; } = [];
    public List<PermissionRule> PermissionRules { get; set; } = [];
}

public class PutSchemaEndpoint : Endpoint<PutSchemaRequest>
{
    private readonly IProjectRepository _projects;

    public PutSchemaEndpoint(IProjectRepository projects)
    {
        _projects = projects;
    }

    public override void Configure()
    {
        Put("/projects/{Id}/schema");
        AllowAnonymous();
    }

    public override async Task HandleAsync(PutSchemaRequest req, CancellationToken ct)
    {
        var project = await _projects.GetByIdAsync(req.Id);
        if (project is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var schema = new SchemaContext
        {
            Tables = req.Tables,
            Relationships = req.Relationships,
            BusinessTerms = req.BusinessTerms,
            CalculationLibrary = req.CalculationLibrary,
            Glossary = req.Glossary,
            PermissionRules = req.PermissionRules
        };

        var json = JsonSerializer.Serialize(schema);
        await _projects.UpdateSchemaContextAsync(req.Id, json);
        await Send.NoContentAsync(ct);
    }
}
