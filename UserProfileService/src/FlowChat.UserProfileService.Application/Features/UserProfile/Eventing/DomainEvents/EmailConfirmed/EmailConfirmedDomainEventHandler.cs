using AutoMapper;
using FlowChat.Shared.Application;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.UserProfileService.Domain.Entities.UserProfile.Events;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Eventing.DomainEvents.EmailConfirmed;

public sealed class EmailConfirmedDomainEventHandler
    : MappedDomainEventHandlerBase<EmailConfirmedDomainEvent, UserEmailConfirmedIntegrationEvent>
{
    public EmailConfirmedDomainEventHandler(
        IOutboxIntegrationEventPublisher integrationEventPublisher,
        IMapper mapper)
        : base(integrationEventPublisher, mapper)
    {
    }
}
