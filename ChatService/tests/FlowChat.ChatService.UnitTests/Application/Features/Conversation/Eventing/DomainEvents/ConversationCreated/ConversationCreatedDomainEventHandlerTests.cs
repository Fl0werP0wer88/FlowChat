using AutoMapper;
using FlowChat.ChatService.Application.Features.Conversation.Eventing.DomainEvents.ConversationCreated;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Domain.Entities.Conversation.Events;
using FlowChat.ChatService.Domain.Entities.UserProfiles;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.ChatService.Events;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.Conversation;

namespace FlowChat.ChatService.UnitTests.Application.Features.Conversation.Eventing.DomainEvents.ConversationCreated;

public sealed class ConversationCreatedDomainEventHandlerTests
{
    private readonly Mock<IOutboxIntegrationEventPublisher> _publisherMock = new();
    private readonly ConversationCreatedDomainEventHandler _handler;

    public ConversationCreatedDomainEventHandlerTests()
    {
        var mapper = new MapperConfiguration(
            cfg => cfg.AddProfile<ConversationCreatedDomainEventToIntegrationEventProfile>(),
            NullLoggerFactory.Instance)
            .CreateMapper();

        _handler = new ConversationCreatedDomainEventHandler(_publisherMock.Object, mapper);
    }

    [Fact]
    public async Task Handle_WhenConversationCreatedDomainEvent_PublishesIntegrationEvent()
    {
        var conversationId = Id<ConversationAggregate>.New();
        var createdByUserId = Id<UserProfile>.New();
        var participantUserIds = new[]
        {
            createdByUserId,
            Id<UserProfile>.New()
        };
        var domainEvent = new ConversationCreatedDomainEvent(
            conversationId,
            ConversationType.Duet,
            null,
            createdByUserId,
            participantUserIds);
        IntegrationEventEnvelope<ConversationChangedIntegrationEvent>? publishedEnvelope = null;

        _publisherMock
            .Setup(x => x.PublishAsync(
                It.IsAny<IntegrationEventEnvelope<ConversationChangedIntegrationEvent>>(),
                It.IsAny<CancellationToken>()))
            .Callback<IntegrationEventEnvelope<ConversationChangedIntegrationEvent>, CancellationToken>(
                (envelope, _) => publishedEnvelope = envelope)
            .Returns(Task.CompletedTask);

        await _handler.Handle(domainEvent, CancellationToken.None);

        publishedEnvelope.Should().NotBeNull();
        publishedEnvelope!.KafkaKey.Should().Be(conversationId.Value.ToString("D"));
        publishedEnvelope.Payload.ConversationId.Should().Be(conversationId.Value);
        publishedEnvelope.Payload.Type.Should().Be((int) ConversationType.Duet);
        publishedEnvelope.Payload.Name.Should().BeNull();
        publishedEnvelope.Payload.CreatedByUserId.Should().Be(createdByUserId.Value);
        publishedEnvelope.Payload.ParticipantUserIds.Should().Equal(participantUserIds.Select(id => id.Value));
        _publisherMock.Verify(
            x => x.PublishAsync(
                It.IsAny<IntegrationEventEnvelope<ConversationChangedIntegrationEvent>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
