using System.Text.RegularExpressions;
using Pidgin;
using Pidgin.Expression;
using Query.Core.Schema;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace Query.Core.Formulas;

public partial class InfixFormulaHandler : IFormulaHandler
{
    public string HandlerName => "Infix";

    [GeneratedRegex(@"[\w)]\s*[+\-*/]\s*[\w(]")]
    private static partial Regex InfixPattern();

    public bool CanHandle(string input)
    {
        return InfixPattern().IsMatch(input);
    }

    public FormulaAST Parse(string input, SchemaContext? context = null)
    {
        var result = ExprParser.Parse(input.Trim());
        if (!result.Success)
        {
            throw new FormatException($"Failed to parse infix expression: {input}");
        }
        return new FormulaAST { Root = result.Value };
    }

    private static Parser<char, T> Tok<T>(Parser<char, T> p) =>
        Try(p).Before(SkipWhitespaces);

    private static Parser<char, char> Tok(char c) =>
        Tok(Char(c));

    private static readonly Parser<char, FormulaNode> Num =
        Tok(
            Real.Select(d => (FormulaNode)new NumberNode((decimal)d))
        );

    private static readonly Parser<char, FormulaNode> Identifier =
        Tok(
            Letter.Then(LetterOrDigit.Or(Char('_')).ManyString(), (first, rest) => first + rest)
                .Select(name => (FormulaNode)new ColumnNode(name))
        );

    private static readonly Parser<char, FormulaNode> ParenExpr =
        Tok('(')
            .Then(Rec(() => ExprParser!))
            .Before(Tok(')'));

    private static readonly Parser<char, FormulaNode> Atom =
        OneOf(
            Try(Num),
            Identifier,
            ParenExpr
        );

    private static Parser<char, Func<FormulaNode, FormulaNode, FormulaNode>> BinOp(char op) =>
        Tok(op).Select<Func<FormulaNode, FormulaNode, FormulaNode>>(
            c => (left, right) => new BinaryOpNode(left, c.ToString(), right)
        );

    private static readonly Parser<char, FormulaNode> ExprParser =
        ExpressionParser.Build(
            Atom,
            new[]
            {
                new[]
                {
                    Operator.InfixL(BinOp('*')),
                    Operator.InfixL(BinOp('/'))
                },
                new[]
                {
                    Operator.InfixL(BinOp('+')),
                    Operator.InfixL(BinOp('-'))
                }
            }
        ).Before(SkipWhitespaces);
}
