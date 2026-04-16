using AutoMapper;
using FlowChat.Shared.Application;
using FlowChat.AuthService.Domain.Entities.Account.Events;
using FlowChat.Core.Messaging.AuthService.Events;

namespace FlowChat.AuthService.Application.Features.Account.Eventing.DomainEvents.AccountRegistered;

public sealed class AccountRegisteredDomainEventHandler
    : MappedDomainEventHandlerBase<AccountRegisteredDomainEvent, AccountRegisteredIntegrationEvent>
{
    public AccountRegisteredDomainEventHandler(
        IOutboxIntegrationEventPublisher integrationEventPublisher,
        IMapper mapper)
        : base(integrationEventPublisher, mapper)
    {
    }
}

