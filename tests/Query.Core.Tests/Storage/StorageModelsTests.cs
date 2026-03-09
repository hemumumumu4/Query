using FluentAssertions;
using Query.Core.Storage;

namespace Query.Core.Tests.Storage;

public class StorageModelsTests
{
    [Fact]
    public void Project_HasRequiredFields()
    {
        var project = new ProjectRecord
        {
            Id = Guid.NewGuid(),
            Name = "Sales Analytics",
            CreatedAt = DateTime.UtcNow
        };

        project.Name.Should().Be("Sales Analytics");
        project.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void ConversationRecord_HasProjectReference()
    {
        var projectId = Guid.NewGuid();
        var conv = new ConversationRecord
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = "user-1",
            CreatedAt = DateTime.UtcNow
        };

        conv.ProjectId.Should().Be(projectId);
    }

    [Fact]
    public void ConversationRecord_DefaultState_IsSchemaLoaded()
    {
        var conv = new ConversationRecord
        {
            UserId = "user-1"
        };

        conv.State.Should().Be("SchemaLoaded");
    }

    [Fact]
    public void ConversationTurnRecord_HasRequiredFields()
    {
        var turn = new ConversationTurnRecord
        {
            Id = Guid.NewGuid(),
            ConversationId = Guid.NewGuid(),
            Role = "user",
            Content = "Show me total sales",
            CreatedAt = DateTime.UtcNow
        };

        turn.Role.Should().Be("user");
        turn.Content.Should().Be("Show me total sales");
    }

    [Fact]
    public void OutputBundleRecord_HasRequiredFields()
    {
        var bundle = new OutputBundleRecord
        {
            Id = Guid.NewGuid(),
            ConversationId = Guid.NewGuid(),
            RawOutput = "SELECT SUM(amount) FROM sales",
            Explanation = "Sums all sales amounts",
            SpecJson = "{}",
            Compiler = "SqlKata",
            Dialect = "PostgreSQL",
            CreatedAt = DateTime.UtcNow
        };

        bundle.Compiler.Should().Be("SqlKata");
        bundle.Dialect.Should().Be("PostgreSQL");
    }
}
