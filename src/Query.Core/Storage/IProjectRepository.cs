namespace Query.Core.Storage;

public interface IProjectRepository
{
    Task<ProjectRecord> CreateAsync(ProjectRecord project);
    Task<ProjectRecord?> GetByIdAsync(Guid id);
    Task UpdateSchemaContextAsync(Guid id, string schemaContextJson);
}
