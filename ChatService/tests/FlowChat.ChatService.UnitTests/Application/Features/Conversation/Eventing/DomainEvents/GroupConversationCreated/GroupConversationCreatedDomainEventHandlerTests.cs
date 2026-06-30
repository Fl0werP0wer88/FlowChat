using AutoMapper;
using FlowChat.ChatService.Application.Features.Conversation.Eventing.DomainEvents.GroupConversationCreated;
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

namespace FlowChat.ChatService.UnitTests.Application.Features.Conversation.Eventing.DomainEvents.GroupConversationCreated;

public sealed class GroupConversationCreatedDomainEventHandlerTests
{
    private readonly Mock<IOutboxIntegrationEventPublisher> _publisherMock = new();
    private readonly GroupConversationCreatedDomainEventHandler _handler;

    public GroupConversationCreatedDomainEventHandlerTests()
    {
        var mapper = new MapperConfiguration(
            cfg => cfg.AddProfile<GroupConversationCreatedDomainEventToIntegrationEventProfile>(),
            NullLoggerFactory.Instance)
            .CreateMapper();

        _handler = new GroupConversationCreatedDomainEventHandler(_publisherMock.Object, mapper);
    }

    [Fact]
    public async Task Handle_WhenGroupConversationCreatedDomainEvent_PublishesIntegrationEvent()
    {
        var conversationId = Id<ConversationAggregate>.New();
        var createdByUserId = Id<UserProfile>.New();
        var participantUserIds = new[]
        {
            createdByUserId,
            Id<UserProfile>.New()
        };
        var domainEvent = new GroupConversationCreatedDomainEvent(
            conversationId,
            ConversationType.Group,
            "Dev Team",
            createdByUserId,
            participantUserIds);
        IntegrationEventEnvelope<GroupConversationChangedIntegrationEvent>? publishedEnvelope = null;

        _publisherMock
            .Setup(x => x.PublishAsync(
                It.IsAny<IntegrationEventEnvelope<GroupConversationChangedIntegrationEvent>>(),
                It.IsAny<CancellationToken>()))
            .Callback<IntegrationEventEnvelope<GroupConversationChangedIntegrationEvent>, CancellationToken>(
                (envelope, _) => publishedEnvelope = envelope)
            .Returns(Task.CompletedTask);

        await _handler.Handle(domainEvent, CancellationToken.None);

        publishedEnvelope.Should().NotBeNull();
        publishedEnvelope!.KafkaKey.Should().Be(conversationId.Value.ToString("D"));
        publishedEnvelope.Payload.ConversationId.Should().Be(conversationId.Value);
        publishedEnvelope.Payload.Type.Should().Be((int) ConversationType.Group);
        publishedEnvelope.Payload.Name.Should().Be("Dev Team");
        publishedEnvelope.Payload.CreatedByUserId.Should().Be(createdByUserId.Value);
        publishedEnvelope.Payload.ParticipantUserIds.Should().Equal(participantUserIds.Select(id => id.Value));
        _publisherMock.Verify(
            x => x.PublishAsync(
                It.IsAny<IntegrationEventEnvelope<GroupConversationChangedIntegrationEvent>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
