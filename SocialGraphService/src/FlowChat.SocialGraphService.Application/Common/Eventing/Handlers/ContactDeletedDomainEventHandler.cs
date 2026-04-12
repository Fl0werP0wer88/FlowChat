using AutoMapper;
using FlowChat.Core.Messaging.SocialGraphService.Events;
using FlowChat.Shared.Application;
using FlowChat.SocialGraphService.Domain.Entities.Contact.Events;

namespace FlowChat.SocialGraphService.Application.Common.Eventing.Handlers;

public sealed class ContactDeletedDomainEventHandler(
    IIntegrationEventPublisher integrationEventPublisher,
    IMapper mapper)
    : MappedDomainEventHandlerBase<ContactDeletedDomainEvent, ContactDeletedIntegrationEvent>(
        integrationEventPublisher,
        mapper);
