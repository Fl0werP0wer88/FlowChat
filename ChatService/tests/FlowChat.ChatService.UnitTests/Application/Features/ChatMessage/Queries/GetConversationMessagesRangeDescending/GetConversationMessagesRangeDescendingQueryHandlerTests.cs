using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.ChatMessage.Dtos;
using FlowChat.ChatService.Application.Features.ChatMessage.Queries.GetConversationMessagesRangeDescending;
using FluentAssertions;
using Moq;

namespace FlowChat.ChatService.UnitTests.Application.Features.ChatMessage.Queries.GetConversationMessagesRangeDescending;

public sealed class GetConversationMessagesRangeDescendingQueryHandlerTests
{
    private readonly Mock<IChatMessageReadRepository> _messages = new();
    private readonly Mock<IConversationMessageSequenceReadRepository> _sequences = new();
    private readonly Mock<IConversationParticipantReadRepository> _participants = new();

    [Fact]
    public async Task Handle_ExplicitRange_ReturnsDescendingPage()
    {
        var conversationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _participants.Setup(x => x.GetParticipantUserIdsAsync(
                conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([userId]);
        _sequences.Setup(x => x.GetCurrentAsync(
                conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(10);
        _messages.Setup(x => x.GetRangeDescendingAsync(
                conversationId, 3, 8, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Message(8), Message(6), Message(4)]);
        var handler = new GetConversationMessagesRangeDescendingQueryHandler(
            _messages.Object, _sequences.Object, _participants.Object);

        var result = await handler.Handle(
            new(conversationId, userId, 3, 8, 2),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Select(item => item.SequenceNum).Should().Equal(8, 6);
        result.Value.StartSequenceNum.Should().Be(3);
        result.Value.EndSequenceNum.Should().Be(8);
        result.Value.HasMore.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NonParticipant_ReturnsUnauthorized()
    {
        var conversationId = Guid.NewGuid();
        _participants.Setup(x => x.GetParticipantUserIdsAsync(
                conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Guid.NewGuid()]);
        var handler = new GetConversationMessagesRangeDescendingQueryHandler(
            _messages.Object, _sequences.Object, _participants.Object);

        var result = await handler.Handle(
            new(conversationId, Guid.NewGuid(), null, null, 50),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(FlowChat.Shared.Domain.ErrorType.Unauthorized);
    }

    private static ChatMessageDto Message(long sequenceNum) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "text", DateTimeOffset.UtcNow, sequenceNum);
}
