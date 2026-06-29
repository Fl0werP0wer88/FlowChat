using AutoMapper;
using FlowChat.ChatService.Application.Features.Conversation.Eventing.DomainEvents.ConversationCreated;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Domain.Entities.Conversation.Events;
using FlowChat.ChatService.Domain.Entities.UserProfiles;
using FlowChat.Core.Messaging.ChatService.Events;
using FlowChat.Shared.Domain;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using ConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.Conversation;

namespace FlowChat.ChatService.UnitTests.Application.Features.Conversation.Eventing.DomainEvents.ConversationCreated;

public sealed class ConversationCreatedDomainEventToIntegrationEventProfileTests
{
    [Fact]
    public void Map_WhenConversationCreatedDomainEvent_ReturnsIntegrationEvent()
    {
        var configuration = new MapperConfiguration(
            cfg => cfg.AddProfile<ConversationCreatedDomainEventToIntegrationEventProfile>(),
            NullLoggerFactory.Instance);
        var mapper = configuration.CreateMapper();
        var conversationId = Id<ConversationAggregate>.New();
        var createdByUserId = Id<UserProfile>.New();
        var participantUserIds = new[]
        {
            createdByUserId,
            Id<UserProfile>.New()
        };
        var domainEvent = new ConversationCreatedDomainEvent(
            conversationId,
            ConversationType.Group,
            "Dev Team",
            createdByUserId,
            participantUserIds);

        var integrationEvent = mapper.Map<ConversationCreatedIntegrationEvent>(domainEvent);

        integrationEvent.ConversationId.Should().Be(conversationId.Value);
        integrationEvent.Type.Should().Be((int) ConversationType.Group);
        integrationEvent.Name.Should().Be("Dev Team");
        integrationEvent.CreatedByUserId.Should().Be(createdByUserId.Value);
        integrationEvent.ParticipantUserIds.Should().Equal(participantUserIds.Select(id => id.Value));
    }
}
