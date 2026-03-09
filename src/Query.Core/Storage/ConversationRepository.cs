using System.Data;
using Dapper;

namespace Query.Core.Storage;

public class ConversationRepository : IConversationRepository
{
    private readonly IDbConnection _connection;

    public ConversationRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<ConversationRecord> CreateAsync(ConversationRecord conversation)
    {
        const string sql = """
            INSERT INTO conversations (id, project_id, user_id, state, current_spec_json, created_at, updated_at)
            VALUES (@Id, @ProjectId, @UserId, @State, @CurrentSpecJson, @CreatedAt, @UpdatedAt)
            RETURNING *;
            """;

        return await _connection.QuerySingleAsync<ConversationRecord>(sql, conversation);
    }

    public async Task<ConversationRecord?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM conversations WHERE id = @Id;";
        return await _connection.QuerySingleOrDefaultAsync<ConversationRecord>(sql, new { Id = id });
    }

    public async Task UpdateStateAsync(Guid id, string state)
    {
        const string sql = "UPDATE conversations SET state = @State, updated_at = @UpdatedAt WHERE id = @Id;";
        await _connection.ExecuteAsync(sql, new { Id = id, State = state, UpdatedAt = DateTime.UtcNow });
    }

    public async Task UpdateStateAndSpecAsync(Guid id, string state, string? specJson)
    {
        const string sql = "UPDATE conversations SET state = @State, current_spec_json = @CurrentSpecJson, updated_at = @UpdatedAt WHERE id = @Id;";
        await _connection.ExecuteAsync(sql, new { Id = id, State = state, CurrentSpecJson = specJson, UpdatedAt = DateTime.UtcNow });
    }

    public async Task AddTurnAsync(ConversationTurnRecord turn)
    {
        const string sql = """
            INSERT INTO conversation_turns (id, conversation_id, role, content, created_at)
            VALUES (@Id, @ConversationId, @Role, @Content, @CreatedAt);
            """;

        await _connection.ExecuteAsync(sql, turn);
    }

    public async Task<IReadOnlyList<ConversationTurnRecord>> GetTurnsAsync(Guid conversationId)
    {
        const string sql = "SELECT * FROM conversation_turns WHERE conversation_id = @ConversationId ORDER BY created_at;";
        var results = await _connection.QueryAsync<ConversationTurnRecord>(sql, new { ConversationId = conversationId });
        return results.ToList().AsReadOnly();
    }

    public async Task SaveOutputBundleAsync(OutputBundleRecord bundle)
    {
        const string sql = """
            INSERT INTO output_bundles (id, conversation_id, raw_output, explanation, spec_json, compiler, dialect, created_at)
            VALUES (@Id, @ConversationId, @RawOutput, @Explanation, @SpecJson, @Compiler, @Dialect, @CreatedAt);
            """;

        await _connection.ExecuteAsync(sql, bundle);
    }
}
