using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.ChatMessage.Commands.SendChatMessage;
using FlowChat.ChatService.Application.Features.Conversation.Dtos;
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
    private readonly Mock<IConversationWriteRepository> _conversationWriteRepositoryMock = new();
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
            _conversationWriteRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object,
            []);
    }

    [Fact]
    public async Task Handle_ConversationNotFound_ReturnsNotFoundFailure()
    {
        var command = new SendChatMessageCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Hello");

        _participantReadRepositoryMock
            .Setup(x => x.GetParticipantStatesAsync(command.ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParticipantStatesResult?)null);

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
        var command = new SendChatMessageCommand(Guid.NewGuid(), conversationId, senderId, "Hello");

        _participantReadRepositoryMock
            .Setup(x => x.GetParticipantStatesAsync(command.ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParticipantStatesResult(1, [
                new ParticipantStateDto(otherUser1, IsBlocked: false, IsHidden: false),
                new ParticipantStateDto(otherUser2, IsBlocked: false, IsHidden: false)
            ]));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Unauthorized);
        result.Error.ErrorMessage.Should().Be("Sender is not a participant of this conversation.");
        _chatMessageRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<ChatMessageAggregate>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_RecipientHasBlockedSender_ReturnsUnauthorizedFailure()
    {
        var senderId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var command = new SendChatMessageCommand(Guid.NewGuid(), conversationId, senderId, "Hello");

        _participantReadRepositoryMock
            .Setup(x => x.GetParticipantStatesAsync(command.ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParticipantStatesResult(1, [
                new ParticipantStateDto(senderId, IsBlocked: false, IsHidden: false),
                new ParticipantStateDto(recipientId, IsBlocked: true, IsHidden: false)
            ]));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Unauthorized);
        result.Error.ErrorMessage.Should().Be("Recipient has blocked this conversation.");
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
        var command = new SendChatMessageCommand(Guid.NewGuid(), conversationId, senderId, "Hello");

        ChatMessageAggregate? persistedMessage = null;
        List<IDomainEvent> dispatchedEvents = [];

        _participantReadRepositoryMock
            .Setup(x => x.GetParticipantStatesAsync(command.ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParticipantStatesResult(3, [
                new ParticipantStateDto(senderId, IsBlocked: false, IsHidden: false),
                new ParticipantStateDto(recipientId, IsBlocked: false, IsHidden: false)
            ]));

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
        persistedMessage.SenderUserId.Value.Should().Be(senderId);
        persistedMessage.RecipientUserIds.Select(x => x.Value).Should().BeEquivalentTo([recipientId]);
        dispatchedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ChatMessageSentDomainEvent>()
            .Which.ConversationVersionAtSend.Should().Be(3);
    }

}

