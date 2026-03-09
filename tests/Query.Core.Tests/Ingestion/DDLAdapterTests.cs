using FluentAssertions;
using Query.Core.Ingestion;

namespace Query.Core.Tests.Ingestion;

public class DDLAdapterTests
{
    private readonly DDLAdapter _adapter = new();

    [Fact]
    public async Task Ingest_ParsesTableAndColumns()
    {
        const string ddl = """
            CREATE TABLE orders (
                id INT PRIMARY KEY,
                customer_id INT NOT NULL,
                revenue DECIMAL(18,2),
                status VARCHAR(50)
            );
            """;

        var ctx = await _adapter.IngestAsync(ddl);

        ctx.Tables.Should().HaveCount(1);
        ctx.Tables[0].Name.Should().Be("orders");
        ctx.Tables[0].Columns.Should().HaveCount(4);
        ctx.Tables[0].Columns.Select(c => c.Name)
            .Should().BeEquivalentTo(["id", "customer_id", "revenue", "status"]);
    }

    [Fact]
    public async Task Ingest_ParsesMultipleTables()
    {
        const string ddl = """
            CREATE TABLE customers (id INT PRIMARY KEY, name VARCHAR(100));
            CREATE TABLE orders (id INT PRIMARY KEY, customer_id INT);
            """;

        var ctx = await _adapter.IngestAsync(ddl);
        ctx.Tables.Should().HaveCount(2);
    }

    [Fact]
    public async Task Ingest_ParsesForeignKeyRelationship()
    {
        const string ddl = """
            CREATE TABLE orders (
                id INT PRIMARY KEY,
                customer_id INT,
                FOREIGN KEY (customer_id) REFERENCES customers(id)
            );
            """;

        var ctx = await _adapter.IngestAsync(ddl);
        ctx.Relationships.Should().HaveCount(1);
        ctx.Relationships[0].FromTable.Should().Be("orders");
        ctx.Relationships[0].ToTable.Should().Be("customers");
    }
}
