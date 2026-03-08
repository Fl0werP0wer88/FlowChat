using AutoMapper;
using FlowChat.Application.Abstractions;
using FlowChat.AuthService.Domain.Events;
using FlowChat.Messaging.Contracts.AuthService.Events;

namespace FlowChat.AuthService.Application.Common.Eventing.Handlers;

public sealed class UserCreatedDomainEventHandler
    : MappedDomainEventHandlerBase<UserCreatedDomainEvent, UserCreatedIntegrationEvent>
{
    public UserCreatedDomainEventHandler(
        IIntegrationEventPublisher integrationEventPublisher,
        IMapper mapper)
        : base(integrationEventPublisher, mapper)
    {
    }
}
