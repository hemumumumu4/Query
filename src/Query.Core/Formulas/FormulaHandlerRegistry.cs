namespace Query.Core.Formulas;

public class FormulaHandlerRegistry
{
    private readonly List<IFormulaHandler> _handlers = [];

    public FormulaHandlerRegistry Register(IFormulaHandler handler)
    {
        _handlers.Add(handler);
        return this;
    }

    public IFormulaHandler? Detect(string input) =>
        _handlers.FirstOrDefault(h => h.CanHandle(input));

    public FormulaAST? TryParse(string input, Query.Core.Schema.SchemaContext? ctx = null)
    {
        var handler = Detect(input);
        return handler?.Parse(input, ctx);
    }
}
