using AutoMapper;
using FlowChat.ChatService.Application.Features.Conversation.Eventing.DomainEvents.GroupConversationCreated;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Domain.Entities.Conversation.Events;
using FlowChat.ChatService.Domain.Entities.UserProfiles;
using FlowChat.Core.Messaging.ChatService.Events;
using FlowChat.Shared.Domain;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using ConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.Conversation;

namespace FlowChat.ChatService.UnitTests.Application.Features.Conversation.Eventing.DomainEvents.GroupConversationCreated;

public sealed class GroupConversationCreatedDomainEventToIntegrationEventProfileTests
{
    [Fact]
    public void Map_WhenGroupConversationCreatedDomainEvent_ReturnsIntegrationEvent()
    {
        var configuration = new MapperConfiguration(
            cfg => cfg.AddProfile<GroupConversationCreatedDomainEventToIntegrationEventProfile>(),
            NullLoggerFactory.Instance);
        var mapper = configuration.CreateMapper();
        var conversationId = Id<ConversationAggregate>.New();
        var createdByUserId = Id<UserProfile>.New();
        var domainEvent = new GroupConversationCreatedDomainEvent(
            conversationId,
            ConversationType.Group,
            "Dev Team",
            createdByUserId);

        var integrationEvent = mapper.Map<GroupConversationChangedIntegrationEvent>(domainEvent);

        integrationEvent.ConversationId.Should().Be(conversationId.Value);
        integrationEvent.Type.Should().Be((int) ConversationType.Group);
        integrationEvent.Name.Should().Be("Dev Team");
        integrationEvent.CreatedByUserId.Should().Be(createdByUserId.Value);
    }
}
