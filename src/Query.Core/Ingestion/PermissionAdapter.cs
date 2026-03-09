using Query.Core.Schema;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Query.Core.Ingestion;

public class PermissionAdapter : ISchemaAdapter
{
    private readonly IDeserializer _deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    public Task<SchemaContext> IngestAsync(string yaml)
    {
        var doc = _deserializer.Deserialize<PermissionDocument>(yaml);
        var rules = doc.Rules
            .Select(r => new PermissionRule(r.Table, r.Filter))
            .ToList();

        return Task.FromResult(new SchemaContext { PermissionRules = rules });
    }

    private class PermissionDocument
    {
        public List<RuleEntry> Rules { get; set; } = [];
    }

    private class RuleEntry
    {
        public string Table { get; set; } = string.Empty;
        public string Filter { get; set; } = string.Empty;
    }
}
