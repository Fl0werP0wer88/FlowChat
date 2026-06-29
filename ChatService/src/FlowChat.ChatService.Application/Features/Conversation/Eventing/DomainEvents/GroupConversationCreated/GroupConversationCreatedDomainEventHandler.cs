using AutoMapper;
using FlowChat.ChatService.Domain.Entities.Conversation.Events;
using FlowChat.Core.Messaging.ChatService.Events;
using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Application.Features.Conversation.Eventing.DomainEvents.GroupConversationCreated;

public sealed class GroupConversationCreatedDomainEventHandler
    : MappedDomainEventHandlerBase<GroupConversationCreatedDomainEvent, GroupConversationChangedIntegrationEvent>
{
    public GroupConversationCreatedDomainEventHandler(
        IOutboxIntegrationEventPublisher integrationEventPublisher,
        IMapper mapper)
        : base(integrationEventPublisher, mapper)
    {
    }

    protected override string ResolveKafkaKey(
        GroupConversationCreatedDomainEvent notification,
        GroupConversationChangedIntegrationEvent integrationEvent) =>
        notification.ConversationId.ToString("D");
}
