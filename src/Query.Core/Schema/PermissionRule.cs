namespace Query.Core.Schema;

public record PermissionRule(string Table, string Filter);

public record PermissionContext(string UserId, List<PermissionRule> Rules)
{
    public string? GetFilterForTable(string tableName) =>
        Rules.FirstOrDefault(r => r.Table.Equals(tableName, StringComparison.OrdinalIgnoreCase))
             ?.Filter.Replace(":user_id", $"'{UserId}'");
}
