using AutoMapper;
using FlowChat.Shared.Application;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.UserProfileService.Domain.Events;

namespace FlowChat.UserProfileService.Application.Common.Eventing.Handlers;

public sealed class UserProfileCreatedDomainEventHandler
    : MappedDomainEventHandlerBase<UserProfileCreatedDomainEvent, UserProfileCreatedIntegrationEvent>
{
    public UserProfileCreatedDomainEventHandler(
        IIntegrationEventPublisher integrationEventPublisher,
        IMapper mapper)
        : base(integrationEventPublisher, mapper)
    {
    }
}

