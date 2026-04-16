using AutoMapper;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.Shared.Application;
using FlowChat.UserProfileService.Domain.Entities.UserProfile.Events;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Eventing.DomainEvents.AuthEmailChanged;

public sealed class AuthEmailChangedDomainEventHandler
    : MappedDomainEventHandlerBase<AuthEmailChangedDomainEvent, AuthEmailChangedIntegrationEvent>
{
    public AuthEmailChangedDomainEventHandler(
        IOutboxIntegrationEventPublisher integrationEventPublisher,
        IMapper mapper)
        : base(integrationEventPublisher, mapper)
    {
    }
}
