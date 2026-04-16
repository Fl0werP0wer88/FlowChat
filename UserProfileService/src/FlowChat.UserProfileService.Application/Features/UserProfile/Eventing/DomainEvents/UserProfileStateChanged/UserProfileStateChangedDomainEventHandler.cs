using AutoMapper;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.UserProfileService.Application.Common.Eventing.Handlers;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;
using DomainUserProfile = FlowChat.UserProfileService.Domain.Entities.UserProfile.UserProfile;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Eventing.DomainEvents.UserProfileStateChanged;

public sealed class UserProfileStateChangedDomainEventHandler
    : MappedDomainEventHandlerBase<
        AggregateStateChangedDomainEvent<DomainUserProfile, UserProfileState>,
        UserProfileChangedIntegrationEvent>
{
    public UserProfileStateChangedDomainEventHandler(
        IOutboxIntegrationEventPublisher integrationEventPublisher,
        IMapper mapper)
        : base(integrationEventPublisher, mapper)
    {
    }
}

