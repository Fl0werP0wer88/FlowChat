using AutoMapper;
using FlowChat.Core.Messaging.SocialGraphService.Events;
using FlowChat.Shared.Application;
using FlowChat.SocialGraphService.Domain.Entities.Contact.Events;

namespace FlowChat.SocialGraphService.Application.Features.Contact.Eventing.DomainEvents.ContactAdded;

public sealed class ContactAddedDomainEventHandler(
    IOutboxIntegrationEventPublisher integrationEventPublisher,
    IMapper mapper)
    : MappedDomainEventHandlerBase<ContactAddedDomainEvent, ContactAddedIntegrationEvent>(
        integrationEventPublisher,
        mapper)
{
    protected override string ResolveKafkaKey(
        ContactAddedDomainEvent notification,
        ContactAddedIntegrationEvent integrationEvent) =>
        notification.AggregateId.ToString();
}
