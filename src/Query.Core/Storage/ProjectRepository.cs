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
