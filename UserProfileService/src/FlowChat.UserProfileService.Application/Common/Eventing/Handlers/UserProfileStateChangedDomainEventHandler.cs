using AutoMapper;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;

namespace FlowChat.UserProfileService.Application.Common.Eventing.Handlers;

public sealed class UserProfileStateChangedDomainEventHandler
    : MappedDomainEventHandlerBase<
        AggregateStateChangedDomainEvent<UserProfile, UserProfileSnapshot>,
        UserProfileStateChangedIntegrationEvent>
{
    public UserProfileStateChangedDomainEventHandler(
        IIntegrationEventPublisher integrationEventPublisher,
        IMapper mapper)
        : base(integrationEventPublisher, mapper)
    {
    }
}

