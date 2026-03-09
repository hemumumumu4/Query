namespace Query.Core.Schema;

public class SchemaContext
{
    public List<TableDef> Tables { get; init; } = [];
    public List<RelationshipDef> Relationships { get; init; } = [];
    public Dictionary<string, string> BusinessTerms { get; init; } = [];
    public Dictionary<string, CalculationEntry> CalculationLibrary { get; init; } = [];
    public Dictionary<string, string> Glossary { get; init; } = [];
    public List<PermissionRule> PermissionRules { get; init; } = [];

    public TableDef? FindTable(string name) =>
        Tables.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public ColumnDef? ResolveAlias(string alias) =>
        Tables.SelectMany(t => t.Columns)
              .FirstOrDefault(c => c.BusinessAliases
                  .Any(a => a.Equals(alias, StringComparison.OrdinalIgnoreCase)));
}

public record TableDef(string Name, string Description, List<ColumnDef> Columns);

public record ColumnDef(
    string Name,
    string Type,
    string Description,
    List<string> BusinessAliases);

public record RelationshipDef(
    string FromTable,
    string FromColumn,
    string ToTable,
    string ToColumn);

public record CalculationEntry(
    string Expression,
    string Description,
    List<string> AppliesTo);
