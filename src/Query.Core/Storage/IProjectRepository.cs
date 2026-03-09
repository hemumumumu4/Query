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
