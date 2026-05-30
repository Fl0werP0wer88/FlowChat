using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.ChatMessage.Commands.SendChatMessage;
using FlowChat.ChatService.Domain.Entities.ChatMessage.Events;
using FlowChat.Core.Messaging;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FluentAssertions;
using Moq;
using ChatMessageAggregate = FlowChat.ChatService.Domain.Entities.ChatMessage.ChatMessage;

namespace FlowChat.ChatService.UnitTests;

public sealed class SendChatMessageCommandHandlerTests
{
    private readonly Mock<IChatMessageWriteRepository> _chatMessageRepositoryMock = new();
    private readonly Mock<IConversationParticipantReadRepository> _participantReadRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ILocalEventDispatcher> _domainEventDispatcherMock = new();
    private readonly SendChatMessageCommandHandler _handler;

    public SendChatMessageCommandHandlerTests()
    {
        _unitOfWorkMock
            .Setup(x => x.ExecuteCommandInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<SendChatMessageCommandResult>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<SendChatMessageCommandResult>>>, CancellationToken>(
                (operation, ct) => operation(ct));

        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new SendChatMessageCommandHandler(
            _chatMessageRepositoryMock.Object,
            _participantReadRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object);
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
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ILocalEvent>, CancellationToken>((events, _) => dispatchedEvents.AddRange(events.OfType<IDomainEvent>()))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.MessageId.Should().NotBeEmpty();
        persistedMessage.Should().NotBeNull();
        result.Value.MessageId.Should().Be(persistedMessage!.Id.Value);
        result.Value.SentAtUtc.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        persistedMessage.ConversationId.Value.Should().Be(conversationId);
        persistedMessage.SenderUserId.Should().Be(senderId);
        persistedMessage.RecipientUserIds.Should().BeEquivalentTo(new[] { recipientId });
        dispatchedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ChatMessageSentDomainEvent>();
    }

}

