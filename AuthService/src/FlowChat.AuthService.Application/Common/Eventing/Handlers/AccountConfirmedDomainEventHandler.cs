using AutoMapper;
using FlowChat.Shared.Application;
using FlowChat.AuthService.Domain.Events;
using FlowChat.Core.Messaging.AuthService.Events;

namespace FlowChat.AuthService.Application.Common.Eventing.Handlers;

public sealed class AccountConfirmedDomainEventHandler
    : MappedDomainEventHandlerBase<AccountConfirmedDomainEvent, UserConfirmedIntegrationEvent>
{
    public AccountConfirmedDomainEventHandler(
        IIntegrationEventPublisher integrationEventPublisher,
        IMapper mapper)
        : base(integrationEventPublisher, mapper)
    {
    }
}

