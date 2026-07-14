using AutoMapper;
using FlowChat.ChatService.Domain.Entities.Conversation.Events;
using FlowChat.Core.Messaging.ChatService.Events;
using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Application.Features.Conversation.Eventing.DomainEvents.GroupConversationParticipantsAdded;

public sealed class GroupConversationParticipantsAddedDomainEventHandler
    : MappedDomainEventHandlerBase<GroupConversationParticipantsAddedDomainEvent, GroupConversationParticipantsAddedIntegrationEvent>
{
    public GroupConversationParticipantsAddedDomainEventHandler(
        IOutboxIntegrationEventPublisher integrationEventPublisher,
        IMapper mapper)
        : base(integrationEventPublisher, mapper)
    {
    }

    protected override string ResolveKafkaKey(
        GroupConversationParticipantsAddedDomainEvent notification,
        GroupConversationParticipantsAddedIntegrationEvent integrationEvent) =>
        notification.ConversationId.ToString("D");
}
