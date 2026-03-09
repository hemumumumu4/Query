using System.Text;
using Query.Core.Domain;
using Query.Core.Schema;

namespace Query.Core.Compilers;

public class MarkdownCompiler : ICompiler
{
    public string Format => "markdown";

    public OutputBundle Compile(QuerySpec spec, PermissionContext permissions)
    {
        var sb = new StringBuilder();

        // Heading
        sb.AppendLine($"# {FormatIntent(spec.Intent)} Report");
        sb.AppendLine();

        // Summary
        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine(GenerateSummary(spec));
        sb.AppendLine();

        // Columns table (dimensions + measures)
        if (spec.Dimensions.Count > 0 || spec.Measures.Count > 0)
        {
            sb.AppendLine("## Columns");
            sb.AppendLine();
            sb.AppendLine("| Column | Type | Expression |");
            sb.AppendLine("|--------|------|------------|");

            foreach (var dim in spec.Dimensions)
            {
                sb.AppendLine($"| {dim.Alias} | dimension | `{dim.Expression}` |");
            }

            foreach (var measure in spec.Measures)
            {
                sb.AppendLine($"| {measure.Alias} | measure | `{measure.Expression}` |");
            }

            sb.AppendLine();
        }

        // Filters section
        if (spec.Filters.Count > 0)
        {
            sb.AppendLine("## Filters");
            sb.AppendLine();
            foreach (var filter in spec.Filters)
            {
                sb.AppendLine($"- `{filter.Expression} {filter.Operator} {filter.Value}`");
            }
            sb.AppendLine();
        }

        // Time range
        if (spec.TimeRange is { } tr)
        {
            sb.AppendLine("## Time Range");
            sb.AppendLine();
            sb.AppendLine($"- **Column:** `{tr.Column}`");
            sb.AppendLine($"- **From:** {tr.From}");
            sb.AppendLine($"- **To:** {tr.To}");
            sb.AppendLine();
        }

        // RLS notice
        if (permissions.Rules.Count > 0)
        {
            sb.AppendLine("> **Note:** Row-level security filters have been applied to this query.");
            sb.AppendLine();
        }

        var rawOutput = sb.ToString().TrimEnd();
        var explanation = GenerateExplanation(spec, permissions);

        return new OutputBundle(
            RawOutput: rawOutput,
            Explanation: explanation,
            Spec: spec,
            Compiler: "markdown",
            Dialect: "markdown"
        );
    }

    private static string FormatIntent(string intent) =>
        string.IsNullOrEmpty(intent)
            ? "Query"
            : char.ToUpperInvariant(intent[0]) + intent[1..];

    private static string GenerateSummary(QuerySpec spec)
    {
        var parts = new List<string>();

        if (spec.Measures.Count > 0)
        {
            var measureNames = spec.Measures.Select(m => m.Alias);
            parts.Add($"Computes **{string.Join("**, **", measureNames)}**");
        }

        if (spec.Entities.Count > 0)
        {
            var tableNames = spec.Entities.Select(e => e.Table);
            parts.Add($"from **{string.Join("**, **", tableNames)}**");
        }

        if (spec.Dimensions.Count > 0)
        {
            var dimNames = spec.Dimensions.Select(d => d.Alias);
            parts.Add($"grouped by **{string.Join("**, **", dimNames)}**");
        }

        return string.Join(" ", parts) + ".";
    }

    internal static string GenerateExplanation(QuerySpec spec, PermissionContext permissions)
    {
        var sb = new StringBuilder();
        sb.Append($"This {spec.Intent} query ");

        if (spec.Measures.Count > 0)
        {
            var measureNames = spec.Measures.Select(m => m.Alias);
            sb.Append($"computes {string.Join(", ", measureNames)} ");
        }

        if (spec.Entities.Count > 0)
        {
            var tableNames = spec.Entities.Select(e => e.Table);
            sb.Append($"from {string.Join(", ", tableNames)}");
        }

        if (spec.Dimensions.Count > 0)
        {
            var dimNames = spec.Dimensions.Select(d => d.Alias);
            sb.Append($", grouped by {string.Join(", ", dimNames)}");
        }

        if (permissions.Rules.Count > 0)
        {
            sb.Append(". Row-level security filters have been applied");
        }

        sb.Append('.');
        return sb.ToString();
    }
}
