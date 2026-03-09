namespace Query.Core.Domain;

public record QuerySpec
{
    public string Version { get; init; } = "1.0";
    public string Intent { get; init; } = string.Empty;
    public string OutputFormat { get; init; } = "sql";
    public List<EntityRef> Entities { get; init; } = [];
    public List<JoinDef> Joins { get; init; } = [];
    public List<MeasureDef> Measures { get; init; } = [];
    public List<DimensionDef> Dimensions { get; init; } = [];
    public List<FilterDef> Filters { get; init; } = [];
    public TimeRange? TimeRange { get; init; }
    public List<CalculationDef> Calculations { get; init; } = [];
    public List<CteDef> Ctes { get; init; } = [];
    public List<OrderByDef> OrderBy { get; init; } = [];
    public int? Limit { get; init; }
    public string PermissionSlot { get; init; } = "__INJECTED_BY_COMPILER__";
}

public record EntityRef(string Table, string Alias);
public record JoinDef(string Table, string Alias, string On);
public record MeasureDef(string Expression, string Alias);
public record DimensionDef(string Expression, string Alias);
public record FilterDef(string Expression, string Operator, string Value);
public record TimeRange(string Column, string From, string To);
public record OrderByDef(string Expression, string Direction);

public record CalculationDef
{
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = "derived";
    public string Expression { get; init; } = string.Empty;
    public FormulaSource? FormulaSource { get; init; }
    public JoinDef? RequiresJoin { get; init; }
    public string? Filter { get; init; }
}

public record FormulaSource(string Handler, string Raw);
public record CteDef(string Name, string Definition);
