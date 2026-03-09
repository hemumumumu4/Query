using FluentAssertions;
using Query.Core.Ingestion;

namespace Query.Core.Tests.Ingestion;

public class MarkdownAdapterTests
{
    private readonly MarkdownAdapter _adapter = new();

    [Fact]
    public async Task Ingest_ExtractsBusinessTerms()
    {
        const string markdown = """
            ## Business Terms
            - **churn**: customers with no activity in 90 days (table: customers)
            - **ltv**: lifetime value of a customer (table: orders)
            """;

        var ctx = await _adapter.IngestAsync(markdown);

        ctx.BusinessTerms.Should().ContainKey("churn");
        ctx.BusinessTerms["churn"].Should().Contain("no activity in 90 days");
    }

    [Fact]
    public async Task Ingest_ExtractsColumnAliases()
    {
        const string markdown = """
            ## Column Aliases
            - orders.revenue: sales, income, turnover
            """;

        var ctx = await _adapter.IngestAsync(markdown);
        ctx.Glossary.Should().ContainKey("sales");
    }
}
