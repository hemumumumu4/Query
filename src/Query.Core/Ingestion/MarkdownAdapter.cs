using System.Text.RegularExpressions;
using Query.Core.Schema;

namespace Query.Core.Ingestion;

public class MarkdownAdapter : ISchemaAdapter
{
    private static readonly Regex TermRegex = new(
        @"-\s+\*\*(\w+)\*\*:\s+(.+)",
        RegexOptions.IgnoreCase);

    private static readonly Regex AliasRegex = new(
        @"-\s+[\w.]+\.(\w+):\s+(.+)",
        RegexOptions.IgnoreCase);

    private static readonly Regex TableHeaderRegex = new(
        @"^###\s+(\w+)",
        RegexOptions.IgnoreCase);

    private static readonly Regex TableRowRegex = new(
        @"^\|\s*(\w+)\s*\|([^|]*)\|([^|]*)\|([^|]*)\|",
        RegexOptions.IgnoreCase);

    private static readonly Regex RelationshipLineRegex = new(
        @"FK\s*→\s*(\w+)\((\w+)\)",
        RegexOptions.IgnoreCase);

    public Task<SchemaContext> IngestAsync(string markdown)
    {
        var businessTerms = new Dictionary<string, string>();
        var glossary = new Dictionary<string, string>();
        var tables = new List<TableDef>();
        var relationships = new List<RelationshipDef>();

        string? currentTableName = null;
        string? currentTableDescription = null;
        var currentColumns = new List<ColumnDef>();
        var inTermsSection = false;
        var inAliasSection = false;

        foreach (var line in markdown.Split('\n'))
        {
            // Detect Business Terms / Column Aliases sections
            if (line.Contains("Business Terms")) { inTermsSection = true; inAliasSection = false; continue; }
            if (line.Contains("Column Aliases")) { inAliasSection = true; inTermsSection = false; continue; }

            // Detect table headers (### table_name)
            var tableMatch = TableHeaderRegex.Match(line);
            if (tableMatch.Success)
            {
                // Save previous table if any
                if (currentTableName != null)
                    tables.Add(new TableDef(currentTableName, currentTableDescription ?? string.Empty, currentColumns));

                currentTableName = tableMatch.Groups[1].Value;
                currentTableDescription = null;
                currentColumns = [];
                inTermsSection = false;
                inAliasSection = false;
                continue;
            }

            // Any other ## header resets sections
            if (line.TrimStart().StartsWith("##"))
            {
                inTermsSection = false;
                inAliasSection = false;
                continue;
            }

            // Capture table description (first non-empty line after ### header, before the | table)
            if (currentTableName != null && currentTableDescription == null
                && !string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith("|"))
            {
                currentTableDescription = line.Trim();
                continue;
            }

            // Parse markdown table rows for column definitions
            if (currentTableName != null)
            {
                var rowMatch = TableRowRegex.Match(line);
                if (rowMatch.Success)
                {
                    var colName = rowMatch.Groups[1].Value.Trim();
                    var colType = rowMatch.Groups[2].Value.Trim();
                    var constraints = rowMatch.Groups[3].Value.Trim();
                    var description = rowMatch.Groups[4].Value.Trim();

                    // Skip header row
                    if (colName.Equals("Column", StringComparison.OrdinalIgnoreCase)) continue;

                    currentColumns.Add(new ColumnDef(colName, colType, description, []));

                    // Extract FK relationships from constraints
                    var fkMatch = RelationshipLineRegex.Match(constraints);
                    if (fkMatch.Success)
                    {
                        relationships.Add(new RelationshipDef(
                            currentTableName,
                            colName,
                            fkMatch.Groups[1].Value,
                            fkMatch.Groups[2].Value));
                    }
                }
            }

            // Business terms
            if (inTermsSection)
            {
                var m = TermRegex.Match(line);
                if (m.Success) businessTerms[m.Groups[1].Value] = m.Groups[2].Value;
            }

            // Column aliases
            if (inAliasSection)
            {
                var m = AliasRegex.Match(line);
                if (m.Success)
                {
                    var columnName = m.Groups[1].Value;
                    foreach (var alias in m.Groups[2].Value.Split(',').Select(a => a.Trim()))
                        glossary[alias] = columnName;
                }
            }
        }

        // Save last table
        if (currentTableName != null)
            tables.Add(new TableDef(currentTableName, currentTableDescription ?? string.Empty, currentColumns));

        return Task.FromResult(new SchemaContext
        {
            Tables = tables,
            Relationships = relationships,
            BusinessTerms = businessTerms,
            Glossary = glossary
        });
    }
}
