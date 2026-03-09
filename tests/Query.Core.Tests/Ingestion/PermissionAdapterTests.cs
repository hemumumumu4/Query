using FluentAssertions;
using Query.Core.Ingestion;

namespace Query.Core.Tests.Ingestion;

public class PermissionAdapterTests
{
    private readonly PermissionAdapter _adapter = new();

    [Fact]
    public async Task Ingest_ParsesPermissionRules()
    {
        const string yaml = """
            rules:
              - table: orders
                filter: "region_id IN (SELECT region_id FROM user_regions WHERE user_id = :user_id)"
              - table: salaries
                filter: "department_id = (SELECT department_id FROM employees WHERE user_id = :user_id)"
            """;

        var ctx = await _adapter.IngestAsync(yaml);

        ctx.PermissionRules.Should().HaveCount(2);
        ctx.PermissionRules[0].Table.Should().Be("orders");
        ctx.PermissionRules[1].Table.Should().Be("salaries");
    }

    [Fact]
    public async Task PermissionContext_ResolvesUserIdInFilter()
    {
        const string yaml = """
            rules:
              - table: orders
                filter: "user_id = :user_id"
            """;

        var ctx = await _adapter.IngestAsync(yaml);
        var permCtx = new Query.Core.Schema.PermissionContext("user-123", ctx.PermissionRules);

        permCtx.GetFilterForTable("orders").Should().Be("user_id = 'user-123'");
    }
}
