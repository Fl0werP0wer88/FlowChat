using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.ChatMessage.Dtos;
using FlowChat.ChatService.Application.Features.ChatMessage.Queries.GetConversationMessagesRangeAscending;
using FluentAssertions;
using Moq;

namespace FlowChat.ChatService.UnitTests.Application.Features.ChatMessage.Queries.GetConversationMessagesRangeAscending;

public sealed class GetConversationMessagesRangeAscendingQueryHandlerTests
{
    private readonly Mock<IChatMessageReadRepository> _messages = new();
    private readonly Mock<IConversationMessageSequenceReadRepository> _sequences = new();
    private readonly Mock<IConversationParticipantReadRepository> _participants = new();

    [Fact]
    public async Task Handle_DefaultRange_UsesOneThroughCurrentAndReturnsAscendingPage()
    {
        var conversationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        ArrangeAccess(conversationId, userId, 5);
        _messages.Setup(x => x.GetRangeAscendingAsync(
                conversationId, 1, 5, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Message(1), Message(3), Message(5)]);

        var result = await Handler().Handle(
            new(conversationId, userId, null, null, 2),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Select(item => item.SequenceNum).Should().Equal(1, 3);
        result.Value.StartSequenceNum.Should().Be(1);
        result.Value.EndSequenceNum.Should().Be(5);
        result.Value.CurrentSequenceNum.Should().Be(5);
        result.Value.HasMore.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_EndAboveCurrent_ClampsRange()
    {
        var conversationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        ArrangeAccess(conversationId, userId, 5);
        _messages.Setup(x => x.GetRangeAscendingAsync(
                conversationId, 2, 5, 101, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await Handler().Handle(
            new(conversationId, userId, 2, 10, 100),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.EndSequenceNum.Should().Be(5);
    }

    [Fact]
    public async Task Handle_StartAboveEffectiveEnd_ReturnsEmptyWithoutRepositoryCall()
    {
        var conversationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        ArrangeAccess(conversationId, userId, 5);

        var result = await Handler().Handle(
            new(conversationId, userId, 6, null, 100),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        _messages.VerifyNoOtherCalls();
    }

    private GetConversationMessagesRangeAscendingQueryHandler Handler() =>
        new(_messages.Object, _sequences.Object, _participants.Object);

    private void ArrangeAccess(Guid conversationId, Guid userId, long current)
    {
        _participants.Setup(x => x.GetParticipantUserIdsAsync(
                conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([userId]);
        _sequences.Setup(x => x.GetCurrentAsync(
                conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(current);
    }

    private static ChatMessageDto Message(long sequenceNum) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "text", DateTimeOffset.UtcNow, sequenceNum);
}
