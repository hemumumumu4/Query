namespace Query.Core.Formulas;

public abstract record FormulaNode;
public record NumberNode(decimal Value) : FormulaNode;
public record ColumnNode(string Name) : FormulaNode;
public record BinaryOpNode(FormulaNode Left, string Op, FormulaNode Right) : FormulaNode;
public record FunctionNode(string Name, List<FormulaNode> Args) : FormulaNode;

public class FormulaAST
{
    public FormulaNode Root { get; init; } = new NumberNode(0);

    public string ToSql(Dictionary<string, string>? columnMap = null)
    {
        return RenderNode(Root, columnMap ?? []);
    }

    private static string RenderNode(FormulaNode node, Dictionary<string, string> map) => node switch
    {
        NumberNode n => n.Value.ToString(),
        ColumnNode c => map.TryGetValue(c.Name, out var mapped) ? mapped : c.Name,
        BinaryOpNode b => $"({RenderNode(b.Left, map)} {b.Op} {RenderNode(b.Right, map)})",
        FunctionNode f => $"{f.Name}({string.Join(", ", f.Args.Select(a => RenderNode(a, map)))})",
        _ => throw new NotSupportedException($"Unknown node type: {node.GetType().Name}")
    };
}
