using AutoMapper;
using FlowChat.Application.Abstractions;
using FlowChat.AuthService.Domain.Events;
using FlowChat.Messaging.Contracts.AuthService.Events;

namespace FlowChat.AuthService.Application.Events;

public sealed class EmailVerificationRequestedDomainEventHandler
    : MappedDomainEventHandlerBase<EmailVerificationRequestedDomainEvent, EmailVerificationRequestIntegrationEvent>
{
    public EmailVerificationRequestedDomainEventHandler(
        IIntegrationEventPublisher integrationEventPublisher,
        IMapper mapper)
        : base(integrationEventPublisher, mapper)
    {
    }
}
