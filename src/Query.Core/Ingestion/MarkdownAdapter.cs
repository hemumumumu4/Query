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

    public Task<SchemaContext> IngestAsync(string markdown)
    {
        var businessTerms = new Dictionary<string, string>();
        var glossary = new Dictionary<string, string>();

        var inTermsSection = false;
        var inAliasSection = false;

        foreach (var line in markdown.Split('\n'))
        {
            if (line.Contains("Business Terms")) { inTermsSection = true; inAliasSection = false; continue; }
            if (line.Contains("Column Aliases")) { inAliasSection = true; inTermsSection = false; continue; }
            if (line.TrimStart().StartsWith("##")) { inTermsSection = false; inAliasSection = false; continue; }

            if (inTermsSection)
            {
                var m = TermRegex.Match(line);
                if (m.Success) businessTerms[m.Groups[1].Value] = m.Groups[2].Value;
            }

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

        return Task.FromResult(new SchemaContext
        {
            BusinessTerms = businessTerms,
            Glossary = glossary
        });
    }
}
