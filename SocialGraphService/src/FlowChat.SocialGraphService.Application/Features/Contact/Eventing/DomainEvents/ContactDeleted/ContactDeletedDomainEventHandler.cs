using AutoMapper;
using FlowChat.Core.Messaging.SocialGraphService.Events;
using FlowChat.Shared.Application;
using FlowChat.SocialGraphService.Domain.Entities.Contact.Events;

namespace FlowChat.SocialGraphService.Application.Features.Contact.Eventing.DomainEvents.ContactDeleted;

public sealed class ContactDeletedDomainEventHandler(
    IOutboxIntegrationEventPublisher integrationEventPublisher,
    IMapper mapper)
    : MappedDomainEventHandlerBase<ContactDeletedDomainEvent, ContactDeletedIntegrationEvent>(
        integrationEventPublisher,
        mapper)
{
    protected override string ResolveKafkaKey(
        ContactDeletedDomainEvent notification,
        ContactDeletedIntegrationEvent integrationEvent) =>
        notification.AggregateId.ToString();
}
