namespace Query.Core.Domain;

public record OutputBundle(
    string RawOutput,
    string Explanation,
    QuerySpec Spec,
    string Compiler,
    string Dialect)
{
    public List<string> Warnings { get; init; } = [];
}
