using AutoMapper;
using FlowChat.Application.Abstractions;
using FlowChat.Messaging.Contracts.UserProfileService.Events;
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
