namespace Query.Core.Storage;

public class ProjectRecord
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? SchemaContextJson { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ConversationRecord
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public required string UserId { get; set; }
    public string State { get; set; } = "SchemaLoaded";
    public string? CurrentSpecJson { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ConversationTurnRecord
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public required string Role { get; set; }
    public required string Content { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class OutputBundleRecord
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public required string RawOutput { get; set; }
    public required string Explanation { get; set; }
    public required string SpecJson { get; set; }
    public required string Compiler { get; set; }
    public required string Dialect { get; set; }
    public DateTime CreatedAt { get; set; }
}
