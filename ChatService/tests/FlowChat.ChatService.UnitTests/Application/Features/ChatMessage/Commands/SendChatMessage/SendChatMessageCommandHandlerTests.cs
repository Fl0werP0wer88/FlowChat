using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.ChatMessage.Commands.SendChatMessage;
using FlowChat.ChatService.Domain.Entities.ChatMessage.Events;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FluentAssertions;
using Moq;
using ChatMessageAggregate = FlowChat.ChatService.Domain.Entities.ChatMessage.ChatMessage;
using ConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.Conversation;

namespace FlowChat.ChatService.UnitTests;

public sealed class SendChatMessageCommandHandlerTests
{
    private readonly Mock<IChatMessageWriteRepository> _chatMessageRepositoryMock = new();
    private readonly Mock<IConversationWriteRepository> _conversationRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IDomainEventDispatcher> _domainEventDispatcherMock = new();
    private readonly SendChatMessageCommandHandler _handler;

    public SendChatMessageCommandHandlerTests()
    {
        _unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<Guid>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<Guid>>>, CancellationToken>(
                (operation, ct) => operation(ct));

        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new SendChatMessageCommandHandler(
            _chatMessageRepositoryMock.Object,
            _conversationRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object);
    }

    [Fact]
    public async Task Handle_ConversationNotFound_ReturnsNotFoundFailure()
    {
        var command = new SendChatMessageCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Alice", "Hello");

        _conversationRepositoryMock
            .Setup(x => x.GetByIdAsync(command.ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConversationAggregate?)null);

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
        var conversationId = Id<ConversationAggregate>.New();
        var command = new SendChatMessageCommand(Guid.NewGuid(), conversationId.Value, senderId, "Alice", "Hello");
        var conversation = ConversationAggregate.Restore(
            conversationId,
            type: ConversationType.Duet,
            name: null,
            createdByUserId: otherUser1,
            participants:
            [
                ParticipantUser.Create(Id<ParticipantUser>.New(), conversationId, otherUser1),
                ParticipantUser.Create(Id<ParticipantUser>.New(), conversationId, otherUser2)
            ]);

        _conversationRepositoryMock
            .Setup(x => x.GetByIdAsync(command.ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

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
        var conversationId = Id<ConversationAggregate>.New();
        var command = new SendChatMessageCommand(Guid.NewGuid(), conversationId.Value, senderId, "Alice", "Hello");
        var conversation = ConversationAggregate.Restore(
            conversationId,
            type: ConversationType.Duet,
            name: null,
            createdByUserId: senderId,
            participants:
            [
                ParticipantUser.Create(Id<ParticipantUser>.New(), conversationId, senderId),
                ParticipantUser.Create(Id<ParticipantUser>.New(), conversationId, recipientId)
            ]);

        ChatMessageAggregate? persistedMessage = null;
        List<IDomainEvent> dispatchedEvents = [];

        _conversationRepositoryMock
            .Setup(x => x.GetByIdAsync(command.ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

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
        result.Value.Should().NotBeEmpty();
        persistedMessage.Should().NotBeNull();
        result.Value.Should().Be(persistedMessage!.Id.Value);
        persistedMessage.ConversationId.Value.Should().Be(conversationId.Value);
        persistedMessage.SenderUserId.Should().Be(senderId);
        persistedMessage.RecipientUserIds.Should().BeEquivalentTo(new[] { recipientId });
        dispatchedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ChatMessageSentDomainEvent>();
    }
}
