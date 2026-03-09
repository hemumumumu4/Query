using Query.Core.Schema;

namespace Query.Core.Formulas;

public interface IFormulaHandler
{
    string HandlerName { get; }
    bool CanHandle(string input);
    FormulaAST Parse(string input, SchemaContext? context = null);
}
