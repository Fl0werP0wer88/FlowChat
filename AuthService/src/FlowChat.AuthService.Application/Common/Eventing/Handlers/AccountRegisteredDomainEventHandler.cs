using AutoMapper;
using FlowChat.Shared.Application;
using FlowChat.AuthService.Domain.Entities.Identity.Events;
using FlowChat.Core.Messaging.AuthService.Events;

namespace FlowChat.AuthService.Application.Common.Eventing.Handlers;

public sealed class AccountRegisteredDomainEventHandler
    : MappedDomainEventHandlerBase<AccountRegisteredDomainEvent, AccountRegisteredIntegrationEvent>
{
    public AccountRegisteredDomainEventHandler(
        IIntegrationEventPublisher integrationEventPublisher,
        IMapper mapper)
        : base(integrationEventPublisher, mapper)
    {
    }
}

