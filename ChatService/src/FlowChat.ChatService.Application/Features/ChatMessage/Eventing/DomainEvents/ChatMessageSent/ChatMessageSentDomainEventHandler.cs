using AutoMapper;
using FlowChat.Shared.Application;
using FlowChat.ChatService.Domain.Entities.ChatMessage.Events;
using FlowChat.Core.Messaging.ChatService.Events;

namespace FlowChat.ChatService.Application.Features.ChatMessage.Eventing.DomainEvents.ChatMessageSent;

public sealed class ChatMessageSentDomainEventHandler
    : MappedDomainEventHandlerBase<ChatMessageSentDomainEvent, ChatMessageSentIntegrationEvent>
{
    public ChatMessageSentDomainEventHandler(
        IOutboxIntegrationEventPublisher integrationEventPublisher,
        IMapper mapper)
        : base(integrationEventPublisher, mapper)
    {
    }
}

