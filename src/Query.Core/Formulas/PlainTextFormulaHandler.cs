using System.Text.RegularExpressions;
using Query.Core.Schema;

namespace Query.Core.Formulas;

public class PlainTextFormulaHandler : IFormulaHandler
{
    public string HandlerName => "plaintext";

    private static readonly Dictionary<string, string> OpMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["plus"] = "+",
        ["minus"] = "-",
        ["divided by"] = "/",
        ["multiplied by"] = "*",
        ["times"] = "*",
        ["over"] = "/"
    };

    private static readonly Regex PlainTextPattern = new(
        @"\b(plus|minus|divided by|multiplied by|times|over)\b",
        RegexOptions.IgnoreCase);

    public bool CanHandle(string input) => PlainTextPattern.IsMatch(input);

    public FormulaAST Parse(string input, SchemaContext? context = null)
    {
        var normalized = Regex.Replace(input, @"divided by", "/dividedby/", RegexOptions.IgnoreCase);
        normalized = Regex.Replace(normalized, @"multiplied by", "/multipliedby/", RegexOptions.IgnoreCase);

        var tokens = Regex.Split(normalized, @"\s+")
            .Select(t => t switch
            {
                "/dividedby/" => "/",
                "/multipliedby/" => "*",
                _ when OpMap.TryGetValue(t, out var op) => op,
                _ => t
            })
            .ToList();

        return new FormulaAST { Root = ParseTokens(tokens, 0).Node };
    }

    private static (FormulaNode Node, int Consumed) ParseTokens(List<string> tokens, int pos)
    {
        if (pos >= tokens.Count)
            throw new FormatException("Unexpected end of formula");

        FormulaNode left = new ColumnNode(tokens[pos]);
        pos++;

        while (pos < tokens.Count && IsOperator(tokens[pos]))
        {
            var op = tokens[pos];
            pos++;
            if (pos >= tokens.Count) throw new FormatException("Expected operand after operator");
            FormulaNode right = new ColumnNode(tokens[pos]);
            pos++;
            left = new BinaryOpNode(left, op, right);
        }

        return (left, pos);
    }

    private static bool IsOperator(string t) => t is "+" or "-" or "*" or "/";
}
