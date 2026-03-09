using FluentAssertions;
using Query.Core.Compilers;
using Query.Core.Domain;
using Query.Core.Schema;

namespace Query.Core.Tests.Compilers;

public class SQLCompilerTests
{
    private readonly SQLCompiler _compiler = new("postgres");

    [Fact]
    public void Compile_SimpleAggregation_ProducesValidSql()
    {
        var spec = new QuerySpec
        {
            Intent = "aggregation",
            Entities = [new EntityRef("orders", "o")],
            Measures = [new MeasureDef("SUM(o.revenue)", "total_revenue")],
            Dimensions = [new DimensionDef("o.region", "region")]
        };
        var permissions = new PermissionContext("user-1", []);

        var bundle = _compiler.Compile(spec, permissions);

        bundle.RawOutput.Should().Contain("SUM");
        bundle.RawOutput.Should().Contain("orders");
        bundle.RawOutput.Should().Contain("region");
        bundle.Dialect.Should().Be("postgres");
    }

    [Fact]
    public void Compile_WithRLSRule_InjectsFilterUnconditionally()
    {
        var spec = new QuerySpec
        {
            Intent = "lookup",
            Entities = [new EntityRef("orders", "o")],
            Measures = [new MeasureDef("COUNT(*)", "count")]
        };
        var permissions = new PermissionContext("user-42", [
            new PermissionRule("orders", "user_id = :user_id")
        ]);

        var bundle = _compiler.Compile(spec, permissions);

        bundle.RawOutput.Should().Contain("user-42");
    }

    [Fact]
    public void Compile_RLSFilter_CannotBeRemovedByEmptyFilters()
    {
        var spec = new QuerySpec
        {
            Intent = "lookup",
            Entities = [new EntityRef("orders", "o")],
            Measures = [new MeasureDef("COUNT(*)", "count")],
            Filters = []
        };
        var permissions = new PermissionContext("user-1", [
            new PermissionRule("orders", "tenant_id = :user_id")
        ]);

        var bundle = _compiler.Compile(spec, permissions);
        bundle.RawOutput.Should().Contain("user-1");
    }

    [Fact]
    public void Compile_WithCte_PrependsWith()
    {
        var spec = new QuerySpec
        {
            Intent = "aggregation",
            Entities = [new EntityRef("refund_window", "rw")],
            Measures = [new MeasureDef("SUM(rw.amount)", "refund_total")],
            Ctes = [new CteDef("refund_window", "SELECT order_id, amount FROM refunds")]
        };
        var permissions = new PermissionContext("user-1", []);

        var bundle = _compiler.Compile(spec, permissions);
        bundle.RawOutput.Should().StartWith("WITH");
        bundle.RawOutput.Should().Contain("refund_window");
    }

    [Fact]
    public void Compile_GeneratesExplanation()
    {
        var spec = new QuerySpec
        {
            Intent = "aggregation",
            Entities = [new EntityRef("orders", "o")],
            Measures = [new MeasureDef("SUM(o.revenue)", "total_revenue")]
        };
        var bundle = _compiler.Compile(spec, new PermissionContext("u", []));
        bundle.Explanation.Should().NotBeNullOrEmpty();
    }
}
