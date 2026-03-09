# Schema Context CRUD Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace ingestion-coupled project creation with clean CRUD endpoints for projects and their schema contexts, with soft-deletes on projects.

**Architecture:** Projects are the top-level resource with a 1:1 schema context stored as JSON. Project creation is simplified to Name/Description only. Schema context is managed via separate PUT (full replace), PATCH (merge keys), and GET endpoints. Soft-delete via `deleted_at` timestamp column.

**Tech Stack:** .NET 10, FastEndpoints 8.0.1, Dapper, PostgreSQL, xUnit + FluentAssertions + Moq

---

### Task 1: DB Schema Migration — Add deleted_at and updated_at to projects

**Files:**
- Modify: `src/Query.Core/Storage/schema.sql:3-9`
- Modify: `src/Query.Core/Storage/StorageModels.cs:3-10`

**Step 1: Update schema.sql**

Add `updated_at` and `deleted_at` columns to the projects table:

```sql
CREATE TABLE IF NOT EXISTS projects (
    id              UUID PRIMARY KEY,
    name            TEXT NOT NULL,
    description     TEXT,
    schema_context_json TEXT,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at      TIMESTAMPTZ
);
```

**Step 2: Update ProjectRecord**

In `StorageModels.cs`, add the new fields to `ProjectRecord`:

```csharp
public class ProjectRecord
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? SchemaContextJson { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
```

**Step 3: Run the migration manually on the database**

```sql
ALTER TABLE projects ADD COLUMN IF NOT EXISTS updated_at TIMESTAMPTZ NOT NULL DEFAULT now();
ALTER TABLE projects ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMPTZ;
```

**Step 4: Build to verify**

Run: `dotnet build src/Query.Api/Query.Api.csproj`
Expected: Build succeeded

**Step 5: Commit**

```bash
git add src/Query.Core/Storage/schema.sql src/Query.Core/Storage/StorageModels.cs
git commit -m "feat: add updated_at and deleted_at columns to projects table"
```

---

### Task 2: Expand IProjectRepository and ProjectRepository

**Files:**
- Modify: `src/Query.Core/Storage/IProjectRepository.cs`
- Modify: `src/Query.Core/Storage/ProjectRepository.cs`

**Step 1: Update IProjectRepository**

Replace the entire interface with:

```csharp
namespace Query.Core.Storage;

public interface IProjectRepository
{
    Task<ProjectRecord> CreateAsync(ProjectRecord project);
    Task<ProjectRecord?> GetByIdAsync(Guid id);
    Task<List<ProjectRecord>> ListAsync();
    Task UpdateAsync(ProjectRecord project);
    Task UpdateSchemaContextAsync(Guid id, string schemaContextJson);
    Task SoftDeleteAsync(Guid id);
}
```

**Step 2: Update ProjectRepository**

Replace the entire class with:

```csharp
using System.Data;
using Dapper;

namespace Query.Core.Storage;

public class ProjectRepository : IProjectRepository
{
    private readonly IDbConnection _connection;

    public ProjectRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<ProjectRecord> CreateAsync(ProjectRecord project)
    {
        const string sql = """
            INSERT INTO projects (id, name, description, created_at, updated_at)
            VALUES (@Id, @Name, @Description, @CreatedAt, @UpdatedAt)
            RETURNING *;
            """;

        return await _connection.QuerySingleAsync<ProjectRecord>(sql, project);
    }

    public async Task<ProjectRecord?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM projects WHERE id = @Id AND deleted_at IS NULL;";
        return await _connection.QuerySingleOrDefaultAsync<ProjectRecord>(sql, new { Id = id });
    }

    public async Task<List<ProjectRecord>> ListAsync()
    {
        const string sql = "SELECT * FROM projects WHERE deleted_at IS NULL ORDER BY created_at DESC;";
        var results = await _connection.QueryAsync<ProjectRecord>(sql);
        return results.ToList();
    }

    public async Task UpdateAsync(ProjectRecord project)
    {
        const string sql = """
            UPDATE projects SET name = @Name, description = @Description, updated_at = @UpdatedAt
            WHERE id = @Id AND deleted_at IS NULL;
            """;
        await _connection.ExecuteAsync(sql, project);
    }

    public async Task UpdateSchemaContextAsync(Guid id, string schemaContextJson)
    {
        const string sql = """
            UPDATE projects SET schema_context_json = @SchemaContextJson, updated_at = now()
            WHERE id = @Id AND deleted_at IS NULL;
            """;
        await _connection.ExecuteAsync(sql, new { Id = id, SchemaContextJson = schemaContextJson });
    }

    public async Task SoftDeleteAsync(Guid id)
    {
        const string sql = "UPDATE projects SET deleted_at = now() WHERE id = @Id AND deleted_at IS NULL;";
        await _connection.ExecuteAsync(sql, new { Id = id });
    }
}
```

**Step 3: Build to verify**

Run: `dotnet build src/Query.Api/Query.Api.csproj`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add src/Query.Core/Storage/IProjectRepository.cs src/Query.Core/Storage/ProjectRepository.cs
git commit -m "feat: expand project repository with list, update, and soft-delete"
```

---

### Task 3: Simplify CreateProjectEndpoint

**Files:**
- Modify: `src/Query.Api/Endpoints/Projects/CreateProjectEndpoint.cs`

**Step 1: Rewrite CreateProjectEndpoint**

Replace the entire file:

```csharp
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
```

**Step 2: Build to verify**

Run: `dotnet build src/Query.Api/Query.Api.csproj`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add src/Query.Api/Endpoints/Projects/CreateProjectEndpoint.cs
git commit -m "feat: simplify create project to name and description only"
```

---

### Task 4: Add ListProjectsEndpoint

**Files:**
- Create: `src/Query.Api/Endpoints/Projects/ListProjectsEndpoint.cs`

**Step 1: Create the endpoint**

```csharp
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
```

**Step 2: Build to verify**

Run: `dotnet build src/Query.Api/Query.Api.csproj`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add src/Query.Api/Endpoints/Projects/ListProjectsEndpoint.cs
git commit -m "feat: add list projects endpoint"
```

---

### Task 5: Add UpdateProjectEndpoint

**Files:**
- Create: `src/Query.Api/Endpoints/Projects/UpdateProjectEndpoint.cs`

**Step 1: Create the endpoint**

```csharp
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
```

**Step 2: Build to verify**

Run: `dotnet build src/Query.Api/Query.Api.csproj`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add src/Query.Api/Endpoints/Projects/UpdateProjectEndpoint.cs
git commit -m "feat: add update project endpoint"
```

---

### Task 6: Add DeleteProjectEndpoint (soft-delete)

**Files:**
- Create: `src/Query.Api/Endpoints/Projects/DeleteProjectEndpoint.cs`

**Step 1: Create the endpoint**

```csharp
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
```

**Step 2: Build to verify**

Run: `dotnet build src/Query.Api/Query.Api.csproj`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add src/Query.Api/Endpoints/Projects/DeleteProjectEndpoint.cs
git commit -m "feat: add soft-delete project endpoint"
```

---

### Task 7: Add Schema Context Endpoints (GET, PUT, PATCH)

**Files:**
- Create: `src/Query.Api/Endpoints/Projects/GetSchemaEndpoint.cs`
- Create: `src/Query.Api/Endpoints/Projects/PutSchemaEndpoint.cs`
- Create: `src/Query.Api/Endpoints/Projects/PatchSchemaEndpoint.cs`

**Step 1: Create GetSchemaEndpoint**

```csharp
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
```

**Step 2: Create PutSchemaEndpoint**

```csharp
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
```

**Step 3: Create PatchSchemaEndpoint**

```csharp
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
```

**Step 4: Build to verify**

Run: `dotnet build src/Query.Api/Query.Api.csproj`
Expected: Build succeeded

**Step 5: Commit**

```bash
git add src/Query.Api/Endpoints/Projects/GetSchemaEndpoint.cs src/Query.Api/Endpoints/Projects/PutSchemaEndpoint.cs src/Query.Api/Endpoints/Projects/PatchSchemaEndpoint.cs
git commit -m "feat: add GET, PUT, PATCH endpoints for schema context"
```

---

### Task 8: Update GetProjectEndpoint to include timestamps

**Files:**
- Modify: `src/Query.Api/Endpoints/Projects/GetProjectEndpoint.cs`

**Step 1: Update the response and handler**

Update `GetProjectResponse` to include timestamps:

```csharp
public class GetProjectResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? SchemaContextJson { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

Update the mapping in `HandleAsync`:

```csharp
await Send.OkAsync(new GetProjectResponse
{
    Id = project.Id,
    Name = project.Name,
    Description = project.Description,
    SchemaContextJson = project.SchemaContextJson,
    CreatedAt = project.CreatedAt,
    UpdatedAt = project.UpdatedAt
}, ct);
```

**Step 2: Build to verify**

Run: `dotnet build src/Query.Api/Query.Api.csproj`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add src/Query.Api/Endpoints/Projects/GetProjectEndpoint.cs
git commit -m "feat: include timestamps in get project response"
```

---

### Task 9: Clean up Program.cs — remove SchemaContextBuilder registration

**Files:**
- Modify: `src/Query.Api/Program.cs:23`

**Step 1: Remove SchemaContextBuilder registration**

Remove this line from `Program.cs`:

```csharp
// Schema ingestion
builder.Services.AddTransient<SchemaContextBuilder>();
```

**Step 2: Build to verify**

Run: `dotnet build src/Query.Api/Query.Api.csproj`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add src/Query.Api/Program.cs
git commit -m "chore: remove SchemaContextBuilder registration from DI"
```

---

### Task 10: Smoke test all endpoints

**Step 1: Start the service**

```bash
cd src/Query.Api && dotnet run &
```

**Step 2: Test POST /projects**

```bash
curl -s -X POST http://localhost:5199/projects \
  -H "Content-Type: application/json" \
  -d '{"Name":"test-project","Description":"A test"}'
```

Expected: 201 with `{"projectId":"<guid>","name":"test-project"}`

**Step 3: Test GET /projects**

```bash
curl -s http://localhost:5199/projects
```

Expected: 200 with projects list

**Step 4: Test PUT /projects/{id}**

```bash
curl -s -X PUT http://localhost:5199/projects/<id> \
  -H "Content-Type: application/json" \
  -d '{"Name":"renamed","Description":"Updated"}'
```

Expected: 204

**Step 5: Test PUT /projects/{id}/schema**

```bash
curl -s -X PUT http://localhost:5199/projects/<id>/schema \
  -H "Content-Type: application/json" \
  -d '{"Tables":[{"Name":"users","Description":"User accounts","Columns":[{"Name":"id","Type":"UUID","Description":"PK","BusinessAliases":[]}]}],"Relationships":[],"BusinessTerms":{},"CalculationLibrary":{},"Glossary":{},"PermissionRules":[]}'
```

Expected: 204

**Step 6: Test GET /projects/{id}/schema**

```bash
curl -s http://localhost:5199/projects/<id>/schema
```

Expected: 200 with the schema JSON

**Step 7: Test PATCH /projects/{id}/schema**

```bash
curl -s -X PATCH http://localhost:5199/projects/<id>/schema \
  -H "Content-Type: application/json" \
  -d '{"BusinessTerms":{"user":"A registered account"}}'
```

Expected: 204

**Step 8: Verify PATCH merged correctly**

```bash
curl -s http://localhost:5199/projects/<id>/schema
```

Expected: Tables still present, BusinessTerms now has the new entry

**Step 9: Test DELETE /projects/{id}**

```bash
curl -s -X DELETE http://localhost:5199/projects/<id>
```

Expected: 204

**Step 10: Verify soft-delete**

```bash
curl -s http://localhost:5199/projects/<id>
```

Expected: 404

```bash
curl -s http://localhost:5199/projects
```

Expected: project no longer in list
