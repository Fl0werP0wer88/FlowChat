using AutoMapper;
using FlowChat.Application.Abstractions;
using FlowChat.ChatService.Domain.Events;
using FlowChat.Messaging.Contracts.ChatService.Events;

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
