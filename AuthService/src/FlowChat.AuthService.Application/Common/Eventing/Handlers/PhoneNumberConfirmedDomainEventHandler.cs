using AutoMapper;
using FlowChat.Application.Abstractions;
using FlowChat.AuthService.Domain.Events;
using FlowChat.Messaging.Contracts.AuthService.Events;

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
