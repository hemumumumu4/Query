using FluentAssertions;
using Query.Core.Schema;

namespace Query.Core.Tests.Schema;

public class SchemaContextTests
{
    [Fact]
    public void SchemaContext_CanLookupTableByName()
    {
        var ctx = new SchemaContext
        {
            Tables =
            [
                new TableDef("orders", "Order records",
                [
                    new ColumnDef("id", "int", "Order ID", []),
                    new ColumnDef("revenue", "decimal", "Order revenue", ["sales", "income"])
                ])
            ]
        };

        var table = ctx.FindTable("orders");
        table.Should().NotBeNull();
        table!.Name.Should().Be("orders");
    }

    [Fact]
    public void SchemaContext_CanResolveBusinessAlias()
    {
        var ctx = new SchemaContext
        {
            Tables =
            [
                new TableDef("orders", "Order records",
                [
                    new ColumnDef("revenue", "decimal", "Revenue", ["sales", "income"])
                ])
            ]
        };

        var col = ctx.ResolveAlias("sales");
        col.Should().NotBeNull();
        col!.Name.Should().Be("revenue");
    }

    [Fact]
    public void SchemaContext_ReturnsNull_ForUnknownTable()
    {
        var ctx = new SchemaContext();
        ctx.FindTable("nonexistent").Should().BeNull();
    }
}
