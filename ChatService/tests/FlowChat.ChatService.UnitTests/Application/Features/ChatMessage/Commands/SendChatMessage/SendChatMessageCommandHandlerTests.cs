using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.ChatMessage.Commands.SendChatMessage;
using FlowChat.ChatService.Domain.Entities.ChatMessage.Events;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using ChatMessageAggregate = FlowChat.ChatService.Domain.Entities.ChatMessage.ChatMessage;

namespace FlowChat.ChatService.UnitTests;

public sealed class SendChatMessageCommandHandlerTests
{
    private readonly Mock<IChatMessageWriteRepository> _chatMessageRepositoryMock = new();
    private readonly Mock<IConversationParticipantReadRepository> _participantReadRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IDomainEventDispatcher> _domainEventDispatcherMock = new();
    private readonly Mock<IDbUpdateExceptionClassifier> _dbUpdateExceptionClassifierMock = new();
    private readonly SendChatMessageCommandHandler _handler;

    public SendChatMessageCommandHandlerTests()
    {
        _unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<IdempotentCommandResult<Guid>>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<IdempotentCommandResult<Guid>>>>, CancellationToken>(
                (operation, ct) => operation(ct));

        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new SendChatMessageCommandHandler(
            _chatMessageRepositoryMock.Object,
            _participantReadRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object,
            _dbUpdateExceptionClassifierMock.Object);
    }

    [Fact]
    public async Task Handle_ConversationNotFound_ReturnsNotFoundFailure()
    {
        var command = new SendChatMessageCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Alice", "Hello");

        _participantReadRepositoryMock
            .Setup(x => x.GetParticipantUserIdsAsync(command.ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Guid>?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);
        result.Error.ErrorMessage.Should().Be("Conversation not found.");
        _chatMessageRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<ChatMessageAggregate>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_SenderNotParticipant_ReturnsUnauthorizedFailure()
    {
        var senderId = Guid.NewGuid();
        var otherUser1 = Guid.NewGuid();
        var otherUser2 = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var command = new SendChatMessageCommand(Guid.NewGuid(), conversationId, senderId, "Alice", "Hello");

        _participantReadRepositoryMock
            .Setup(x => x.GetParticipantUserIdsAsync(command.ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([otherUser1, otherUser2]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Unauthorized);
        result.Error.ErrorMessage.Should().Be("Sender is not a participant of this conversation.");
        _chatMessageRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<ChatMessageAggregate>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsSuccessAndDispatchesChatMessageSentEvent()
    {
        var senderId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var command = new SendChatMessageCommand(Guid.NewGuid(), conversationId, senderId, "Alice", "Hello");

        ChatMessageAggregate? persistedMessage = null;
        List<IDomainEvent> dispatchedEvents = [];

        _participantReadRepositoryMock
            .Setup(x => x.GetParticipantUserIdsAsync(command.ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([senderId, recipientId]);

        _chatMessageRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ChatMessageAggregate>(), It.IsAny<CancellationToken>()))
            .Callback<ChatMessageAggregate, CancellationToken>((msg, _) => persistedMessage = msg)
            .ReturnsAsync((ChatMessageAggregate msg, CancellationToken _) => msg);

        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<IDomainEvent>, CancellationToken>((events, _) => dispatchedEvents.AddRange(events))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.WasAlreadyProcessed.Should().BeFalse();
        result.Value.Value.Should().NotBeEmpty();
        persistedMessage.Should().NotBeNull();
        result.Value.Value.Should().Be(persistedMessage!.Id.Value);
        persistedMessage.ConversationId.Value.Should().Be(conversationId);
        persistedMessage.SenderUserId.Should().Be(senderId);
        persistedMessage.RecipientUserIds.Should().BeEquivalentTo(new[] { recipientId });
        dispatchedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ChatMessageSentDomainEvent>();
    }

    [Fact]
    public async Task Handle_WhenMessageIdAlreadyExists_ReturnsExistingResponseWithoutDispatchingEvents()
    {
        var senderId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var command = new SendChatMessageCommand(Guid.NewGuid(), conversationId, senderId, "Alice", "Hello");
        var existingMessage = ChatMessageAggregate.Create(
            Id<ChatMessageAggregate>.FromGuid(command.Id),
            Id<FlowChat.ChatService.Domain.Entities.Conversation.Conversation>.FromGuid(conversationId),
            senderId,
            "Alice",
            "Hello",
            [recipientId]);
        existingMessage.ClearEvents();
        List<IDomainEvent> dispatchedEvents = [];

        _participantReadRepositoryMock
            .Setup(x => x.GetParticipantUserIdsAsync(command.ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([senderId, recipientId]);
        _chatMessageRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ChatMessageAggregate>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("duplicate"));
        _dbUpdateExceptionClassifierMock
            .Setup(x => x.IsExpectedIdempotencyConflict(It.IsAny<DbUpdateException>(), SendChatMessageCommand.IdempotencyConflictKey))
            .Returns(true);
        _chatMessageRepositoryMock
            .Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingMessage);

        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<IDomainEvent>, CancellationToken>((events, _) => dispatchedEvents.AddRange(events))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.WasAlreadyProcessed.Should().BeTrue();
        result.Value.Value.Should().Be(command.Id);
        dispatchedEvents.Should().BeEmpty();
    }
}
