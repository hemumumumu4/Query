using System.Text.RegularExpressions;
using Query.Core.Schema;

namespace Query.Core.Ingestion;

public class DDLAdapter : ISchemaAdapter
{
    private static readonly Regex TableRegex = new(
        @"CREATE\s+TABLE\s+(\w+)\s*\(([^;]+)\)",
        RegexOptions.IgnoreCase | RegexOptions.Singleline);

    private static readonly Regex ColumnRegex = new(
        @"^\s*(\w+)\s+(\w+[\w(),]*)",
        RegexOptions.IgnoreCase);

    private static readonly Regex FkRegex = new(
        @"FOREIGN\s+KEY\s*\((\w+)\)\s+REFERENCES\s+(\w+)\s*\((\w+)\)",
        RegexOptions.IgnoreCase);

    public Task<SchemaContext> IngestAsync(string ddl)
    {
        var tables = new List<TableDef>();
        var relationships = new List<RelationshipDef>();

        foreach (Match tableMatch in TableRegex.Matches(ddl))
        {
            var tableName = tableMatch.Groups[1].Value;
            var body = tableMatch.Groups[2].Value;
            var columns = new List<ColumnDef>();

            foreach (var line in body.Split('\n'))
            {
                var trimmed = line.Trim().TrimEnd(',');
                if (string.IsNullOrWhiteSpace(trimmed)) continue;

                var fkMatch = FkRegex.Match(trimmed);
                if (fkMatch.Success)
                {
                    relationships.Add(new RelationshipDef(
                        tableName,
                        fkMatch.Groups[1].Value,
                        fkMatch.Groups[2].Value,
                        fkMatch.Groups[3].Value));
                    continue;
                }

                if (Regex.IsMatch(trimmed, @"^\s*(PRIMARY|UNIQUE|CHECK|CONSTRAINT|INDEX)", RegexOptions.IgnoreCase))
                    continue;

                var colMatch = ColumnRegex.Match(trimmed);
                if (colMatch.Success)
                {
                    columns.Add(new ColumnDef(
                        colMatch.Groups[1].Value,
                        colMatch.Groups[2].Value,
                        string.Empty,
                        []));
                }
            }

            tables.Add(new TableDef(tableName, string.Empty, columns));
        }

        return Task.FromResult(new SchemaContext { Tables = tables, Relationships = relationships });
    }
}
