namespace Query.Core.Conversation;

public enum ConversationState
{
    SchemaLoaded,
    IntentCapture,
    Disambiguation,
    SpecConfirmed,
    Done
}
