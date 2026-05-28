using AutoMapper;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.Shared.Application;
using FlowChat.UserProfileService.Domain.Entities.UserProfile.Events;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Eventing.DomainEvents.UserProfileDeleted;

public sealed class UserProfileDeletedDomainEventHandler(
    IOutboxIntegrationEventPublisher integrationEventPublisher,
    IMapper mapper)
    : MappedDomainEventHandlerBase<UserProfileDeletedDomainEvent, UserProfileDeletedIntegrationEvent>(
        integrationEventPublisher,
        mapper)
{
    protected override string ResolveKafkaKey(
        UserProfileDeletedDomainEvent notification,
        UserProfileDeletedIntegrationEvent integrationEvent) =>
        notification.UserProfileId.Value.ToString();
}
