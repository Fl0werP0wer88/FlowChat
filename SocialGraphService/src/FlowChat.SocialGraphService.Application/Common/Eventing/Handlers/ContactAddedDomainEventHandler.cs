using AutoMapper;
using FlowChat.Core.Messaging.SocialGraphService.Events;
using FlowChat.Shared.Application;
using FlowChat.SocialGraphService.Domain.Entities.Contact.Events;

namespace FlowChat.SocialGraphService.Application.Common.Eventing.Handlers;

public sealed class ContactAddedDomainEventHandler(
    IOutboxIntegrationEventPublisher integrationEventPublisher,
    IMapper mapper)
    : MappedDomainEventHandlerBase<ContactAddedDomainEvent, ContactAddedIntegrationEvent>(
        integrationEventPublisher,
        mapper);
