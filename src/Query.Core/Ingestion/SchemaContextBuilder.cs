using Query.Core.Schema;

namespace Query.Core.Ingestion;

public class SchemaContextBuilder
{
    private readonly Dictionary<string, ISchemaAdapter> _registry = [];
    private readonly List<(string AdapterKey, string Input)> _pending = [];

    public SchemaContextBuilder Register(string key, ISchemaAdapter adapter)
    {
        _registry[key] = adapter;
        return this;
    }

    public SchemaContextBuilder Add(string adapterKey, string input)
    {
        _pending.Add((adapterKey, input));
        return this;
    }

    public async Task<SchemaContext> BuildAsync()
    {
        var contexts = new List<SchemaContext>();

        foreach (var (key, input) in _pending)
        {
            if (!_registry.TryGetValue(key, out var adapter))
                throw new InvalidOperationException($"No adapter registered for key '{key}'");
            contexts.Add(await adapter.IngestAsync(input));
        }

        return Merge(contexts);
    }

    private static SchemaContext Merge(List<SchemaContext> contexts) => new()
    {
        Tables = contexts.SelectMany(c => c.Tables).ToList(),
        Relationships = contexts.SelectMany(c => c.Relationships).ToList(),
        BusinessTerms = contexts.SelectMany(c => c.BusinessTerms)
            .ToDictionary(k => k.Key, v => v.Value),
        CalculationLibrary = contexts.SelectMany(c => c.CalculationLibrary)
            .ToDictionary(k => k.Key, v => v.Value),
        Glossary = contexts.SelectMany(c => c.Glossary)
            .ToDictionary(k => k.Key, v => v.Value),
        PermissionRules = contexts.SelectMany(c => c.PermissionRules).ToList()
    };
}
