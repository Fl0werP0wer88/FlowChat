using AutoFixture;
using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.ChatMessage.Commands.SendChatMessage;
using FlowChat.ChatService.Domain.Entities.ChatMessage;
using FlowChat.ChatService.Domain.Entities.ChatMessage.Events;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Domain.Entities.UserProfiles;
using FlowChat.Core.Messaging;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FluentAssertions;
using Moq;

namespace FlowChat.ChatService.UnitTests.Application.Features.ChatMessage.Commands.SendChatMessage;

public sealed class SendChatMessageCommandHandlerV2Tests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IChatMessageV2WriteRepository> _messageRepository = new();
    private readonly Mock<IConversationMessageSequenceRepositoryV2> _sequenceRepository = new();
    private readonly Mock<IConversationParticipantWriteRepository> _participantRepository = new();
    private readonly Mock<IConversationV2WriteRepository> _conversationRepository = new();
    private readonly Mock<ILocalEventDispatcher> _dispatcher = new();
    private readonly SendChatMessageCommandHandlerV2 _handler;

    public SendChatMessageCommandHandlerV2Tests()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(x => x.ExecuteCommandInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<SendChatMessageCommandResultV2>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<SendChatMessageCommandResultV2>>>, CancellationToken>(
                (operation, cancellationToken) => operation(cancellationToken));

        _messageRepository
            .Setup(x => x.AddAsync(It.IsAny<ChatMessageV2>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChatMessageV2 message, CancellationToken _) => message);
        _dispatcher
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new SendChatMessageCommandHandlerV2(
            _messageRepository.Object,
            _sequenceRepository.Object,
            _participantRepository.Object,
            _conversationRepository.Object,
            unitOfWork.Object,
            _dispatcher.Object,
            []);
    }

    [Fact]
    public async Task Handle_WhenRequestIsValid_AssignsSequenceAndDispatchesMatchingEvent()
    {
        var command = CreateCommand();
        var conversationId = Id<ConversationV2>.FromGuid(command.ConversationId);
        var senderUserId = Id<UserProfile>.FromGuid(command.SenderUserId);
        var recipientUserId = Id<UserProfile>.FromGuid(_fixture.Create<Guid>());
        var conversation = ConversationV2.Restore(
            conversationId,
            ConversationType.Group,
            "Friends",
            duetParticipants: null);
        var participants = new[]
        {
            CreateParticipant(conversationId, senderUserId),
            CreateParticipant(conversationId, recipientUserId)
        };
        const long sequenceNum = 42;
        ChatMessageV2? savedMessage = null;
        List<ILocalEvent> dispatchedEvents = [];

        _conversationRepository
            .Setup(x => x.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);
        _participantRepository
            .Setup(x => x.GetActiveByConversationIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(participants);
        _sequenceRepository
            .Setup(x => x.GetNextAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sequenceNum);
        _messageRepository
            .Setup(x => x.AddAsync(It.IsAny<ChatMessageV2>(), It.IsAny<CancellationToken>()))
            .Callback<ChatMessageV2, CancellationToken>((message, _) => savedMessage = message)
            .ReturnsAsync((ChatMessageV2 message, CancellationToken _) => message);
        _dispatcher
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ILocalEvent>, CancellationToken>((events, _) => dispatchedEvents.AddRange(events))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.MessageId.Should().Be(command.Id);
        result.Value.SequenceNum.Should().Be(sequenceNum);
        savedMessage.Should().NotBeNull();
        savedMessage!.SequenceNum.Should().Be(sequenceNum);
        dispatchedEvents.OfType<ChatMessageSentDomainEventV2>().Should().ContainSingle()
            .Which.SequenceNum.Should().Be(sequenceNum);
        _sequenceRepository.Verify(
            x => x.GetNextAsync(conversationId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenConversationDoesNotExist_DoesNotAllocateSequence()
    {
        var command = CreateCommand();
        _conversationRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Id<ConversationV2>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConversationV2?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);
        _sequenceRepository.Verify(
            x => x.GetNextAsync(It.IsAny<Id<ConversationV2>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSenderIsNotParticipant_DoesNotAllocateSequence()
    {
        var command = CreateCommand();
        var conversationId = Id<ConversationV2>.FromGuid(command.ConversationId);
        var senderUserId = Id<UserProfile>.FromGuid(command.SenderUserId);
        var conversation = ConversationV2.Restore(
            conversationId,
            ConversationType.Group,
            "Friends",
            duetParticipants: null);
        _conversationRepository
            .Setup(x => x.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);
        _participantRepository
            .Setup(x => x.GetActiveByConversationIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                CreateParticipant(conversationId, Id<UserProfile>.FromGuid(_fixture.Create<Guid>()))
            ]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Unauthorized);
        _sequenceRepository.Verify(
            x => x.GetNextAsync(It.IsAny<Id<ConversationV2>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private SendChatMessageCommandV2 CreateCommand() =>
        new(
            _fixture.Create<Guid>(),
            _fixture.Create<Guid>(),
            _fixture.Create<Guid>(),
            _fixture.Create<string>());

    private static ConversationParticipant CreateParticipant(
        Id<ConversationV2> conversationId,
        Id<UserProfile> userId) =>
        ConversationParticipant.Create(
            Id<ConversationParticipant>.New(),
            conversationId,
            ConversationType.Group,
            userId,
            duetPartnerUserId: null);
}
