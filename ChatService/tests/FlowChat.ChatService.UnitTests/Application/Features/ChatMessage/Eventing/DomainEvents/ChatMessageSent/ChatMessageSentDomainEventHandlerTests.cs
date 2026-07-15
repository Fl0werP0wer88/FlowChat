using AutoMapper;
using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.ChatMessage.Eventing.DomainEvents.ChatMessageSent;
using FlowChat.ChatService.Domain.Entities.ChatMessage.Events;
using FlowChat.ChatService.Domain.Entities.UserProfiles;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.ChatService.Events;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ChatMessageAggregate = FlowChat.ChatService.Domain.Entities.ChatMessage.ChatMessage;
using ConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.Conversation;

namespace FlowChat.ChatService.UnitTests.Application.Features.ChatMessage.Eventing.DomainEvents.ChatMessageSent;

public sealed class ChatMessageSentDomainEventHandlerTests
{
    private readonly Mock<IOutboxIntegrationEventPublisher> _publisherMock = new();
    private readonly Mock<IConversationParticipantReadRepository> _participantReadRepositoryMock = new();
    private readonly ChatMessageSentDomainEventHandler _handler;

    public ChatMessageSentDomainEventHandlerTests()
    {
        var mapper = new MapperConfiguration(
            cfg => cfg.AddProfile<ChatMessageSentDomainEventToIntegrationEventProfile>(),
            NullLoggerFactory.Instance)
            .CreateMapper();

        _handler = new ChatMessageSentDomainEventHandler(
            _publisherMock.Object,
            mapper,
            _participantReadRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenChatMessageSentDomainEvent_StampsFreshlyReadMembershipRevision()
    {
        var messageId = Id<ChatMessageAggregate>.New();
        var conversationId = Id<ConversationAggregate>.New();
        var senderUserId = Id<UserProfile>.New();
        var sentAtUtc = UtcDateTimeOffset.UtcNow;
        var domainEvent = new ChatMessageSentDomainEvent(
            messageId,
            conversationId,
            senderUserId,
            "Hello there",
            sentAtUtc);

        _participantReadRepositoryMock
            .Setup(x => x.GetMembershipRevisionAsync(conversationId.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        IntegrationEventEnvelope<ChatMessageSentIntegrationEvent>? publishedEnvelope = null;
        _publisherMock
            .Setup(x => x.PublishAsync(
                It.IsAny<IntegrationEventEnvelope<ChatMessageSentIntegrationEvent>>(),
                It.IsAny<CancellationToken>()))
            .Callback<IntegrationEventEnvelope<ChatMessageSentIntegrationEvent>, CancellationToken>(
                (envelope, _) => publishedEnvelope = envelope)
            .Returns(Task.CompletedTask);

        await _handler.Handle(domainEvent, CancellationToken.None);

        publishedEnvelope.Should().NotBeNull();
        publishedEnvelope!.Payload.MessageId.Should().Be(messageId.Value);
        publishedEnvelope.Payload.ConversationId.Should().Be(conversationId.Value);
        publishedEnvelope.Payload.SenderUserId.Should().Be(senderUserId.Value);
        publishedEnvelope.Payload.ConversationMembershipRevision.Should().Be(5);
        _participantReadRepositoryMock.Verify(
            x => x.GetMembershipRevisionAsync(conversationId.Value, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenMembershipRevisionIsUnknown_StampsZero()
    {
        var domainEvent = new ChatMessageSentDomainEvent(
            Id<ChatMessageAggregate>.New(),
            Id<ConversationAggregate>.New(),
            Id<UserProfile>.New(),
            "Hello there",
            UtcDateTimeOffset.UtcNow);

        _participantReadRepositoryMock
            .Setup(x => x.GetMembershipRevisionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int?)null);

        IntegrationEventEnvelope<ChatMessageSentIntegrationEvent>? publishedEnvelope = null;
        _publisherMock
            .Setup(x => x.PublishAsync(
                It.IsAny<IntegrationEventEnvelope<ChatMessageSentIntegrationEvent>>(),
                It.IsAny<CancellationToken>()))
            .Callback<IntegrationEventEnvelope<ChatMessageSentIntegrationEvent>, CancellationToken>(
                (envelope, _) => publishedEnvelope = envelope)
            .Returns(Task.CompletedTask);

        await _handler.Handle(domainEvent, CancellationToken.None);

        publishedEnvelope.Should().NotBeNull();
        publishedEnvelope!.Payload.ConversationMembershipRevision.Should().Be(0);
    }
}
