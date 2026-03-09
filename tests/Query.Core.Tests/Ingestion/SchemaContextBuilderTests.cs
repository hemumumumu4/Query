using FluentAssertions;
using Query.Core.Ingestion;

namespace Query.Core.Tests.Ingestion;

public class SchemaContextBuilderTests
{
    [Fact]
    public async Task Builder_MergesMultipleAdapterOutputs()
    {
        var builder = new SchemaContextBuilder()
            .Register("ddl", new DDLAdapter())
            .Register("markdown", new MarkdownAdapter());

        const string ddl = "CREATE TABLE orders (id INT PRIMARY KEY, revenue DECIMAL(18,2));";
        const string markdown = "## Business Terms\n- **revenue**: total sales amount";

        var ctx = await builder
            .Add("ddl", ddl)
            .Add("markdown", markdown)
            .BuildAsync();

        ctx.Tables.Should().HaveCount(1);
        ctx.Tables[0].Name.Should().Be("orders");
        ctx.BusinessTerms.Should().ContainKey("revenue");
    }
}
