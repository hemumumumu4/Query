namespace Query.Core.Storage;

public interface IConversationRepository
{
    Task<ConversationRecord> CreateAsync(ConversationRecord conversation);
    Task<ConversationRecord?> GetByIdAsync(Guid id);
    Task UpdateStateAsync(Guid id, string state);
    Task AddTurnAsync(ConversationTurnRecord turn);
    Task<IReadOnlyList<ConversationTurnRecord>> GetTurnsAsync(Guid conversationId);
    Task SaveOutputBundleAsync(OutputBundleRecord bundle);
}
