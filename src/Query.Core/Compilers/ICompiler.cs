using Query.Core.Domain;
using Query.Core.Schema;

namespace Query.Core.Compilers;

public interface ICompiler
{
    string Format { get; }
    OutputBundle Compile(QuerySpec spec, PermissionContext permissions);
}
