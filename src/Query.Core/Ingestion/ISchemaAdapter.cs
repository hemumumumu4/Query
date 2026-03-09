using Query.Core.Schema;

namespace Query.Core.Ingestion;

public interface ISchemaAdapter
{
    Task<SchemaContext> IngestAsync(string input);
}
