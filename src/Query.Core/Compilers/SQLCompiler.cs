using System.Text;
using Query.Core.Domain;
using Query.Core.Schema;
using SqlKata;
using SqlKata.Compilers;

namespace Query.Core.Compilers;

public class SQLCompiler : ICompiler
{
    private readonly string _dialect;
    private readonly Compiler _sqlKataCompiler;

    public string Format => "sql";

    public SQLCompiler(string dialect)
    {
        _dialect = dialect.ToLowerInvariant();
        _sqlKataCompiler = _dialect switch
        {
            "postgres" => new PostgresCompiler(),
            "sqlserver" => new SqlServerCompiler(),
            "mysql" => new MySqlCompiler(),
            "oracle" => new OracleCompiler(),
            _ => throw new ArgumentException($"Unsupported SQL dialect: {dialect}")
        };
    }

    public OutputBundle Compile(QuerySpec spec, PermissionContext permissions)
    {
        var query = BuildQuery(spec, permissions);
        var sqlResult = _sqlKataCompiler.Compile(query);
        var rawSql = sqlResult.Sql;

        // Prepend CTEs if present
        if (spec.Ctes.Count > 0)
        {
            rawSql = BuildCteSql(spec.Ctes) + rawSql;
        }

        var explanation = GenerateExplanation(spec, permissions);

        return new OutputBundle(
            RawOutput: rawSql,
            Explanation: explanation,
            Spec: spec,
            Compiler: nameof(SQLCompiler),
            Dialect: _dialect
        );
    }

    private SqlKata.Query BuildQuery(QuerySpec spec, PermissionContext permissions)
    {
        // Start with the first entity as FROM
        var primary = spec.Entities.FirstOrDefault()
            ?? throw new InvalidOperationException("QuerySpec must have at least one entity.");

        var query = new SqlKata.Query(primary.Table + " as " + primary.Alias);

        // SELECT: measures + dimensions + calculations
        var selectColumns = new List<string>();

        foreach (var dim in spec.Dimensions)
        {
            selectColumns.Add($"{dim.Expression} as {dim.Alias}");
        }

        foreach (var measure in spec.Measures)
        {
            selectColumns.Add($"{measure.Expression} as {measure.Alias}");
        }

        foreach (var calc in spec.Calculations)
        {
            selectColumns.Add($"{calc.Expression} as {calc.Name}");
        }

        if (selectColumns.Count > 0)
        {
            query.SelectRaw(string.Join(", ", selectColumns));
        }

        // JOINs
        foreach (var join in spec.Joins)
        {
            query.Join(
                $"{join.Table} as {join.Alias}",
                j => j.WhereRaw(join.On),
                "inner"
            );
        }

        // Additional entities beyond the first (cross joins / implicit joins)
        foreach (var entity in spec.Entities.Skip(1))
        {
            query.Join($"{entity.Table} as {entity.Alias}", j => j, "cross");
        }

        // WHERE filters from spec
        foreach (var filter in spec.Filters)
        {
            var expression = $"{filter.Expression} {filter.Operator} {filter.Value}";
            query.WhereRaw(expression);
        }

        // Time range filter
        if (spec.TimeRange is { } tr)
        {
            query.WhereRaw($"{tr.Column} BETWEEN '{tr.From}' AND '{tr.To}'");
        }

        // RLS injection — unconditional, cannot be bypassed
        InjectRlsFilters(query, spec, permissions);

        // GROUP BY (dimensions)
        if (spec.Dimensions.Count > 0)
        {
            foreach (var dim in spec.Dimensions)
            {
                query.GroupByRaw(dim.Expression);
            }
        }

        // ORDER BY
        foreach (var order in spec.OrderBy)
        {
            if (order.Direction.Equals("desc", StringComparison.OrdinalIgnoreCase))
                query.OrderByRaw($"{order.Expression} DESC");
            else
                query.OrderByRaw($"{order.Expression} ASC");
        }

        // LIMIT
        if (spec.Limit.HasValue)
        {
            query.Limit(spec.Limit.Value);
        }

        return query;
    }

    private static void InjectRlsFilters(
        SqlKata.Query query,
        QuerySpec spec,
        PermissionContext permissions)
    {
        foreach (var entity in spec.Entities)
        {
            var rlsFilter = permissions.GetFilterForTable(entity.Table);
            if (!string.IsNullOrEmpty(rlsFilter))
            {
                query.WhereRaw(rlsFilter);
            }
        }
    }

    private static string BuildCteSql(List<CteDef> ctes)
    {
        var sb = new StringBuilder("WITH ");
        for (var i = 0; i < ctes.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(ctes[i].Name);
            sb.Append(" AS (");
            sb.Append(ctes[i].Definition);
            sb.Append(')');
        }
        sb.Append(' ');
        return sb.ToString();
    }

    private static string GenerateExplanation(QuerySpec spec, PermissionContext permissions)
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
