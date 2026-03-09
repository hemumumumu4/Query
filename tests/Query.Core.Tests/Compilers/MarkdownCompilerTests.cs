using FluentAssertions;
using Query.Core.Compilers;
using Query.Core.Domain;
using Query.Core.Schema;

namespace Query.Core.Tests.Compilers;

public class MarkdownCompilerTests
{
    private readonly MarkdownCompiler _compiler = new();

    [Fact]
    public void Compile_ProducesMarkdownWithHeaders()
    {
        var spec = new QuerySpec
        {
            Intent = "aggregation",
            Entities = [new EntityRef("orders", "o")],
            Measures = [new MeasureDef("SUM(o.revenue)", "total_revenue")],
            Dimensions = [new DimensionDef("o.region", "region")]
        };

        var bundle = _compiler.Compile(spec, new PermissionContext("u", []));

        bundle.RawOutput.Should().Contain("# ");
        bundle.RawOutput.Should().Contain("total_revenue");
        bundle.RawOutput.Should().Contain("region");
        bundle.Compiler.Should().Be("markdown");
    }
}
