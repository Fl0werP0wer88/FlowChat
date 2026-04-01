using AutoMapper;
using FlowChat.Shared.Application;
using FlowChat.AuthService.Domain.Events;
using FlowChat.Core.Messaging.AuthService.Events;

namespace FlowChat.AuthService.Application.Common.Eventing.Handlers;

public sealed class PhoneNumberConfirmedDomainEventHandler
    : MappedDomainEventHandlerBase<PhoneNumberConfirmedDomainEvent, PhoneNumberConfirmedIntegrationEvent>
{
    public PhoneNumberConfirmedDomainEventHandler(
        IIntegrationEventPublisher integrationEventPublisher,
        IMapper mapper)
        : base(integrationEventPublisher, mapper)
    {
    }
}

