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
            INSERT INTO projects (id, name, description, schema_context_json, created_at)
            VALUES (@Id, @Name, @Description, @SchemaContextJson, @CreatedAt)
            RETURNING *;
            """;

        return await _connection.QuerySingleAsync<ProjectRecord>(sql, project);
    }

    public async Task<ProjectRecord?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM projects WHERE id = @Id;";
        return await _connection.QuerySingleOrDefaultAsync<ProjectRecord>(sql, new { Id = id });
    }

    public async Task UpdateSchemaContextAsync(Guid id, string schemaContextJson)
    {
        const string sql = "UPDATE projects SET schema_context_json = @SchemaContextJson WHERE id = @Id;";
        await _connection.ExecuteAsync(sql, new { Id = id, SchemaContextJson = schemaContextJson });
    }
}
