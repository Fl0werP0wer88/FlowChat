using AutoMapper;
using FlowChat.ChatService.Domain.Entities.Conversation.Events;
using FlowChat.Core.Messaging.ChatService.Events;
using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Application.Features.Conversation.Eventing.DomainEvents.ConversationCreated;

public sealed class ConversationCreatedDomainEventHandler
    : MappedDomainEventHandlerBase<ConversationCreatedDomainEvent, ConversationCreatedIntegrationEvent>
{
    public ConversationCreatedDomainEventHandler(
        IOutboxIntegrationEventPublisher integrationEventPublisher,
        IMapper mapper)
        : base(integrationEventPublisher, mapper)
    {
    }

    protected override string ResolveKafkaKey(
        ConversationCreatedDomainEvent notification,
        ConversationCreatedIntegrationEvent integrationEvent) =>
        notification.ConversationId.ToString("D");
}
