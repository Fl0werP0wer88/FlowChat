using AutoMapper;
using FlowChat.Shared.Application;
using FlowChat.AuthService.Domain.Entities.Account.Events;
using FlowChat.Core.Messaging.AuthService.Events;

namespace FlowChat.AuthService.Application.Common.Eventing.Handlers;

public sealed class AccountConfirmedDomainEventHandler
    : MappedDomainEventHandlerBase<AccountConfirmedDomainEvent, AccountConfirmedIntegrationEvent>
{
    public AccountConfirmedDomainEventHandler(
        IOutboxIntegrationEventPublisher integrationEventPublisher,
        IMapper mapper)
        : base(integrationEventPublisher, mapper)
    {
    }
}

