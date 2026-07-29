using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.ChatMessage.Dtos;
using FlowChat.ChatService.Application.Features.ChatMessage.Queries.GetConversationMessages;
using FlowChat.Shared.Domain;
using FluentAssertions;
using Moq;

namespace FlowChat.ChatService.UnitTests.Application.Features.ChatMessage.Queries.GetConversationMessages;

public sealed class GetConversationMessagesQueryHandlerTests
{
    private readonly Mock<IChatMessageReadRepository> _messages = new();
    private readonly Mock<IConversationMessageSequenceReadRepository> _sequences = new();
    private readonly Mock<IConversationParticipantReadRepository> _participants = new();
    private readonly GetConversationMessagesQueryHandler _handler;

    public GetConversationMessagesQueryHandlerTests()
    {
        _handler = new(_messages.Object, _sequences.Object, _participants.Object);
    }

    [Fact]
    public async Task Handle_ConversationNotFound_ReturnsNotFound()
    {
        var query = Query(Guid.NewGuid(), Guid.NewGuid());
        _participants
            .Setup(x => x.GetParticipantUserIdsAsync(
                query.ConversationId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Guid>?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);
        _sequences.Verify(
            x => x.GetCurrentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_NonParticipant_ReturnsUnauthorized()
    {
        var query = Query(Guid.NewGuid(), Guid.NewGuid());
        _participants
            .Setup(x => x.GetParticipantUserIdsAsync(
                query.ConversationId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([Guid.NewGuid()]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Handle_History_ReturnsSequenceCursorAndCurrentBoundary()
    {
        var conversationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var query = Query(conversationId, userId, limit: 2, beforeSequenceNum: 8);
        _participants
            .Setup(x => x.GetParticipantUserIdsAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([userId]);
        _sequences
            .Setup(x => x.GetCurrentAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(10);
        _messages
            .Setup(x => x.GetBeforeSequenceAsync(
                conversationId,
                10,
                8,
                3,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([Message(7), Message(6), Message(5)]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Select(message => message.SequenceNum).Should().Equal(7, 6);
        result.Value.NextBeforeSequenceNum.Should().Be(6);
        result.Value.CurrentSequenceNum.Should().Be(10);
        result.Value.HasMore.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_EmptyConversation_ReturnsEmptyHistoryAtZero()
    {
        var conversationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var query = Query(conversationId, userId);
        _participants
            .Setup(x => x.GetParticipantUserIdsAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([userId]);
        _sequences
            .Setup(x => x.GetCurrentAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((long?)null);
        _messages
            .Setup(x => x.GetBeforeSequenceAsync(
                conversationId,
                0,
                null,
                51,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.CurrentSequenceNum.Should().Be(0);
        result.Value.NextBeforeSequenceNum.Should().BeNull();
        result.Value.HasMore.Should().BeFalse();
    }

    private static GetConversationMessagesQuery Query(
        Guid conversationId,
        Guid userId,
        int limit = 50,
        long? beforeSequenceNum = null) =>
        new(conversationId, userId, limit, beforeSequenceNum);

    private static ChatMessageDto Message(long sequenceNum) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "message", DateTimeOffset.UtcNow, sequenceNum);
}
