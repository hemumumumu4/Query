using FluentAssertions;
using Query.Core.Domain;

namespace Query.Core.Tests.Domain;

public class QuerySpecTests
{
    [Fact]
    public void QuerySpec_DefaultVersion_IsOnePointZero()
    {
        var spec = new QuerySpec();
        spec.Version.Should().Be("1.0");
    }

    [Fact]
    public void QuerySpec_PermissionSlot_HasSentinelValue()
    {
        var spec = new QuerySpec();
        spec.PermissionSlot.Should().Be("__INJECTED_BY_COMPILER__");
    }

    [Fact]
    public void OutputBundle_RequiresRawOutputAndSpec()
    {
        var spec = new QuerySpec();
        var bundle = new OutputBundle("SELECT 1", "Returns one row", spec, "sql", "postgres");
        bundle.RawOutput.Should().Be("SELECT 1");
        bundle.Compiler.Should().Be("sql");
        bundle.Dialect.Should().Be("postgres");
        bundle.Warnings.Should().BeEmpty();
    }
}
