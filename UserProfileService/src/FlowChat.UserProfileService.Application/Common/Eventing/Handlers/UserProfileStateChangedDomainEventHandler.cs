using AutoMapper;
using FlowChat.Application.Abstractions;
using FlowChat.Domain.Abstractions;
using FlowChat.Messaging.Contracts.UserProfileService.Events;
using FlowChat.UserProfileService.Domain.Entities;

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
