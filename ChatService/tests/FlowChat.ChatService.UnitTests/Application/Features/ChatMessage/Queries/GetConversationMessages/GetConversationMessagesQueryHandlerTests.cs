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
        _handler = new(
            _messages.Object,
            _sequences.Object,
            _participants.Object);
    }

    [Fact]
    public async Task Handle_ConversationNotFound_ReturnsNotFound()
    {
        var query = Query(Guid.NewGuid(), Guid.NewGuid());
        _participants.Setup(x => x.GetParticipantUserIdsAsync(
                query.ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Guid>?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);
        _sequences.Verify(x => x.GetCurrentAsync(
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NonParticipant_ReturnsUnauthorized()
    {
        var query = Query(Guid.NewGuid(), Guid.NewGuid());
        _participants.Setup(x => x.GetParticipantUserIdsAsync(
                query.ConversationId, It.IsAny<CancellationToken>()))
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
        var query = new GetConversationMessagesQuery(
            conversationId, userId, 2, 20, null, null);
        Authorize(conversationId, userId, currentSequenceNum: 30);
        _messages.Setup(x => x.GetBeforeSequenceAsync(
                conversationId, 30, 20, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                Message(conversationId, userId, 19),
                Message(conversationId, userId, 18),
                Message(conversationId, userId, 17)
            ]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Select(item => item.SequenceNum).Should().Equal(19, 18);
        result.Value.NextBeforeSequenceNum.Should().Be(18);
        result.Value.NextAfterSequenceNum.Should().BeNull();
        result.Value.CurrentSequenceNum.Should().Be(30);
        result.Value.ThroughSequenceNum.Should().BeNull();
        result.Value.HasMore.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_CatchUp_FreezesThroughAndReturnsAfterCursor()
    {
        var conversationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var query = new GetConversationMessagesQuery(
            conversationId, userId, 2, null, 20, null);
        Authorize(conversationId, userId, currentSequenceNum: 30);
        _messages.Setup(x => x.GetAfterSequenceAsync(
                conversationId, 20, 30, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                Message(conversationId, userId, 21),
                Message(conversationId, userId, 23),
                Message(conversationId, userId, 24)
            ]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Select(item => item.SequenceNum).Should().Equal(21, 23);
        result.Value.NextAfterSequenceNum.Should().Be(23);
        result.Value.ThroughSequenceNum.Should().Be(30);
        result.Value.HasMore.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ThroughAboveCurrent_ReturnsBadRequest()
    {
        var conversationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        Authorize(conversationId, userId, currentSequenceNum: 30);

        var result = await _handler.Handle(
            new GetConversationMessagesQuery(
                conversationId, userId, 100, null, 20, 31),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.BadRequest);
    }

    [Fact]
    public async Task Handle_AfterAboveCurrentWithoutThrough_ReturnsBadRequest()
    {
        var conversationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _participants
            .Setup(x => x.GetParticipantUserIdsAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([userId]);
        _sequences
            .Setup(x => x.GetCurrentAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(10);

        var result = await _handler.Handle(
            new GetConversationMessagesQuery(conversationId, userId, 50, null, 11, null),
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

    [Fact]
    public async Task Handle_EmptyCatchUp_CompletesAtBoundary()
    {
        var conversationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        Authorize(conversationId, userId, currentSequenceNum: 30);
        _messages.Setup(x => x.GetAfterSequenceAsync(
                conversationId, 20, 30, 101, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _handler.Handle(
            new GetConversationMessagesQuery(
                conversationId, userId, 100, null, 20, 30),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.ThroughSequenceNum.Should().Be(30);
        result.Value.HasMore.Should().BeFalse();
    }

    private void Authorize(Guid conversationId, Guid userId, long currentSequenceNum)
    {
        _participants.Setup(x => x.GetParticipantUserIdsAsync(
                conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([userId]);
        _sequences.Setup(x => x.GetCurrentAsync(
                conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentSequenceNum);
    }

    private static GetConversationMessagesQuery Query(Guid conversationId, Guid userId) =>
        new(conversationId, userId, 50, null, null, null);

    private static ChatMessageDto Message(
        Guid conversationId,
        Guid senderId,
        long sequenceNum) =>
        new(
            Guid.NewGuid(),
            conversationId,
            senderId,
            $"Message {sequenceNum}",
            DateTimeOffset.UtcNow,
            sequenceNum);
}
