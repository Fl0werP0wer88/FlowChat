using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.ChatMessage.Dtos;
using FlowChat.ChatService.Application.Features.ChatMessage.Queries.CatchUpConversationMessages;
using FlowChat.Shared.Domain;
using FluentAssertions;
using Moq;

namespace FlowChat.ChatService.UnitTests.Application.Features.ChatMessage.Queries.CatchUpConversationMessages;

public sealed class CatchUpConversationMessagesQueryHandlerTests
{
    private readonly Mock<IChatMessageReadRepository> _messages = new();
    private readonly Mock<IConversationMessageSequenceReadRepository> _sequences = new();
    private readonly Mock<IConversationParticipantReadRepository> _participants = new();
    private readonly CatchUpConversationMessagesQueryHandler _handler;

    public CatchUpConversationMessagesQueryHandlerTests()
    {
        _handler = new(_messages.Object, _sequences.Object, _participants.Object);
    }

    [Fact]
    public async Task Handle_ConversationNotFound_ReturnsNotFound()
    {
        var query = Query(Guid.NewGuid(), Guid.NewGuid(), 0, null);
        _participants
            .Setup(x => x.GetParticipantUserIdsAsync(
                query.ConversationId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Guid>?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_NonParticipant_ReturnsUnauthorized()
    {
        var query = Query(Guid.NewGuid(), Guid.NewGuid(), 0, null);
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
    public async Task Handle_CatchUp_FreezesThroughAndReturnsAfterCursor()
    {
        var conversationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var query = Query(conversationId, userId, 10, null, limit: 2);
        ArrangeParticipantAndCurrent(conversationId, userId, 30);
        _messages
            .Setup(x => x.GetAfterSequenceAsync(
                conversationId,
                10,
                30,
                3,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([Message(11), Message(13), Message(15)]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Select(message => message.SequenceNum).Should().Equal(11, 13);
        result.Value.NextAfterSequenceNum.Should().Be(13);
        result.Value.CurrentSequenceNum.Should().Be(30);
        result.Value.ThroughSequenceNum.Should().Be(30);
        result.Value.HasMore.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ExplicitThrough_PreservesSnapshotBoundary()
    {
        var conversationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var query = Query(conversationId, userId, 10, 20);
        ArrangeParticipantAndCurrent(conversationId, userId, 30);
        _messages
            .Setup(x => x.GetAfterSequenceAsync(
                conversationId,
                10,
                20,
                101,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ThroughSequenceNum.Should().Be(20);
        result.Value.CurrentSequenceNum.Should().Be(30);
        result.Value.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ThroughAboveCurrent_ReturnsBadRequest()
    {
        var conversationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        ArrangeParticipantAndCurrent(conversationId, userId, 20);

        var result = await _handler.Handle(
            Query(conversationId, userId, 10, 21),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.BadRequest);
    }

    [Fact]
    public async Task Handle_AfterAboveCurrentWithoutThrough_ReturnsBadRequest()
    {
        var conversationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        ArrangeParticipantAndCurrent(conversationId, userId, 10);

        var result = await _handler.Handle(
            Query(conversationId, userId, 11, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.BadRequest);
        _messages.Verify(
            x => x.GetAfterSequenceAsync(
                It.IsAny<Guid>(),
                It.IsAny<long>(),
                It.IsAny<long>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private void ArrangeParticipantAndCurrent(Guid conversationId, Guid userId, long current)
    {
        _participants
            .Setup(x => x.GetParticipantUserIdsAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([userId]);
        _sequences
            .Setup(x => x.GetCurrentAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(current);
    }

    private static CatchUpConversationMessagesQuery Query(
        Guid conversationId,
        Guid userId,
        long after,
        long? through,
        int limit = 100) =>
        new(conversationId, userId, limit, after, through);

    private static ChatMessageDto Message(long sequenceNum) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "message", DateTimeOffset.UtcNow, sequenceNum);
}
