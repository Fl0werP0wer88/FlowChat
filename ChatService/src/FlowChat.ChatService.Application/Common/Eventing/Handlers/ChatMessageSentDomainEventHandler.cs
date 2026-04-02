using AutoMapper;
using FlowChat.Shared.Application;
using FlowChat.ChatService.Domain.Entities.ChatMessage.Events;
using FlowChat.Core.Messaging.ChatService.Events;

namespace FlowChat.ChatService.Application.Common.Eventing.Handlers;

public sealed class ChatMessageSentDomainEventHandler
    : MappedDomainEventHandlerBase<ChatMessageSentDomainEvent, ChatMessageSentIntegrationEvent>
{
    public ChatMessageSentDomainEventHandler(
        IIntegrationEventPublisher integrationEventPublisher,
        IMapper mapper)
        : base(integrationEventPublisher, mapper)
    {
    }
}

