using FluentAssertions;
using Moq;
using Query.Core.Conversation;
using Query.Core.Domain;
using Query.Core.LLM;
using Query.Core.Schema;

namespace Query.Core.Tests.Conversation;

public class ConversationSessionTests
{
    private readonly Mock<ILLMProvider> _llm = new();

    [Fact]
    public void NewSession_StartsInSchemaLoadedState()
    {
        var session = CreateSession();
        session.State.Should().Be(ConversationState.SchemaLoaded);
    }

    [Fact]
    public async Task SendMessage_TransitionsToIntentCapture_OnFirstMessage()
    {
        _llm.Setup(l => l.CompleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<LLMOptions>()))
            .ReturnsAsync("""{"intent":"aggregation","entities":[{"table":"orders","alias":"o"}],"measures":[{"expression":"SUM(o.revenue)","alias":"total_revenue"}],"dimensions":[],"filters":[],"clarification_needed":false}""");

        var session = CreateSession();
        var response = await session.SendMessageAsync("Show me total revenue");

        session.State.Should().BeOneOf(ConversationState.IntentCapture, ConversationState.Disambiguation, ConversationState.SpecConfirmed);
        response.Should().NotBeNull();
    }

    [Fact]
    public async Task SendMessage_AsksClarification_WhenAmbiguous()
    {
        _llm.Setup(l => l.CompleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<LLMOptions>()))
            .ReturnsAsync("""{"intent":"aggregation","entities":[{"table":"orders","alias":"o"}],"measures":[],"dimensions":[],"filters":[],"clarification_needed":true,"clarification_question":"Which revenue column — gross or net?"}""");

        var session = CreateSession();
        var response = await session.SendMessageAsync("Show me revenue");

        session.State.Should().Be(ConversationState.Disambiguation);
        response.Message.Should().Contain("revenue");
    }

    private ConversationSession CreateSession() => new(
        _llm.Object,
        new SchemaContext { Tables = [] },
        new PermissionContext("user-1", []));
}
