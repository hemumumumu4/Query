namespace Query.Core.Compilers;

public class CompilerRegistry
{
    private readonly Dictionary<string, ICompiler> _compilers = new(StringComparer.OrdinalIgnoreCase);

    public void Register(string format, ICompiler compiler)
    {
        _compilers[format] = compiler;
    }

    public ICompiler Resolve(string format)
    {
        if (_compilers.TryGetValue(format, out var compiler))
            return compiler;

        throw new KeyNotFoundException($"No compiler registered for format '{format}'.");
    }

    public IReadOnlyCollection<string> SupportedFormats => _compilers.Keys;

    public static CompilerRegistry CreateDefault(string sqlDialect = "postgres")
    {
        var registry = new CompilerRegistry();
        registry.Register("sql", new SQLCompiler(sqlDialect));
        return registry;
    }
}
