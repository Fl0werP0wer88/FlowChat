using AutoMapper;
using FlowChat.ChatService.Domain.Entities.Conversation.Events;
using FlowChat.Core.Messaging.ChatService.Events;
using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Application.Features.Conversation.Eventing.DomainEvents.GroupConversationParticipantsRemoved;

public sealed class GroupConversationParticipantsRemovedDomainEventHandler
    : MappedDomainEventHandlerBase<GroupConversationParticipantsRemovedDomainEvent, GroupConversationParticipantsRemovedIntegrationEvent>
{
    public GroupConversationParticipantsRemovedDomainEventHandler(
        IOutboxIntegrationEventPublisher integrationEventPublisher,
        IMapper mapper)
        : base(integrationEventPublisher, mapper)
    {
    }

    protected override string ResolveKafkaKey(
        GroupConversationParticipantsRemovedDomainEvent notification,
        GroupConversationParticipantsRemovedIntegrationEvent integrationEvent) =>
        notification.ConversationId.ToString("D");
}
