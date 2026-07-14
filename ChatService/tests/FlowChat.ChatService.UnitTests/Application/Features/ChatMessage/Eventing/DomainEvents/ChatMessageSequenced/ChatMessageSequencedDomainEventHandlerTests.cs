using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.ChatMessage.Eventing.DomainEvents.ChatMessageSequenced;
using FlowChat.ChatService.Domain.Entities.ChatMessage.Events;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using FluentAssertions;
using Moq;
using ChatMessageAggregate = FlowChat.ChatService.Domain.Entities.ChatMessage.ChatMessage;
using ConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.Conversation;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.UnitTests.Application.Features.ChatMessage.Eventing.DomainEvents.ChatMessageSequenced;

public sealed class ChatMessageSequencedDomainEventHandlerTests
{
    private readonly Mock<IConversationWriteRepository> _conversationRepositoryMock = new();
    private readonly Mock<ILocalEventDispatcher> _localEventDispatcherMock = new();
    private readonly Mock<IAggregateBeforeSaveProcessor<ChatMessageSequencedDomainEvent, ConversationAggregate>> _beforeSaveProcessorMock = new();
    private readonly ChatMessageSequencedDomainEventHandler _handler;

    public ChatMessageSequencedDomainEventHandlerTests()
    {
        _localEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _beforeSaveProcessorMock
            .Setup(x => x.ProcessAsync(
                It.IsAny<ChatMessageSequencedDomainEvent>(),
                It.IsAny<ConversationAggregate>(),
                It.IsAny<MutationType>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new ChatMessageSequencedDomainEventHandler(
            _conversationRepositoryMock.Object,
            _localEventDispatcherMock.Object,
            [_beforeSaveProcessorMock.Object]);
    }

    [Fact]
    public async Task Handle_WhenConversationExistsAndSequenceNumberIsGreater_UpdatesConversationSequenceNumber()
    {
        var conversation = CreateGroupConversation();
        var notification = CreateNotification(conversation.Id, sequenceNum: 42);

        _conversationRepositoryMock
            .Setup(x => x.GetByIdAsync(notification.ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        await _handler.Handle(notification, CancellationToken.None);

        conversation.LastMsgSequenceNum.Should().Be(42);
        _beforeSaveProcessorMock.Verify(
            x => x.ProcessAsync(notification, conversation, MutationType.Updated, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSequenceNumberEqualsCurrent_DoesNotMarkConversationAsUpdated()
    {
        var conversation = CreateGroupConversation();
        conversation.SetSequenceNumber(42);
        var initialVersion = conversation.Version;
        var notification = CreateNotification(conversation.Id, sequenceNum: 42);

        _conversationRepositoryMock
            .Setup(x => x.GetByIdAsync(notification.ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        await _handler.Handle(notification, CancellationToken.None);

        conversation.LastMsgSequenceNum.Should().Be(42);
        conversation.Version.Should().Be(initialVersion);
        _beforeSaveProcessorMock.Verify(
            x => x.ProcessAsync(
                It.IsAny<ChatMessageSequencedDomainEvent>(),
                It.IsAny<ConversationAggregate>(),
                It.IsAny<MutationType>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenConversationIsMissing_ThrowsResultException()
    {
        var notification = CreateNotification(Id<ConversationAggregate>.New(), sequenceNum: 42);

        _conversationRepositoryMock
            .Setup(x => x.GetByIdAsync(notification.ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConversationAggregate?)null);

        var act = () => _handler.Handle(notification, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ResultException>();
        exception.Which.Result.Error.ErrorType.Should().Be(ErrorType.NotFound);
    }

    private static ChatMessageSequencedDomainEvent CreateNotification(
        Id<ConversationAggregate> conversationId,
        long sequenceNum)
    {
        return new ChatMessageSequencedDomainEvent(
            Id<ChatMessageAggregate>.New(),
            conversationId,
            sequenceNum);
    }

    private static GroupConversation CreateGroupConversation()
    {
        var creatorId = Id<UserProfileMarker>.New();
        var memberId = Id<UserProfileMarker>.New();

        return GroupConversation.Create(
            Id<ConversationAggregate>.New(),
            creatorId,
            [creatorId, memberId],
            "Dev Team");
    }
}
