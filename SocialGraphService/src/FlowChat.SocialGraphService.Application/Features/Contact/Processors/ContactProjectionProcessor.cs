using AutoMapper;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.SocialGraphService.ReadModels;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using ContactAggregate = FlowChat.SocialGraphService.Domain.Entities.Contact.Contact;

namespace FlowChat.SocialGraphService.Application.Features.Contact.Processors;

public sealed class ContactProjectionProcessor<TCommand>(
    IMapper mapper,
    IOutboxIntegrationEventPublisher integrationEventPublisher)
    : PublishProjectionIntegrationEventProcessor<TCommand, ContactAggregate, ContactReadModel>(
        mapper,
        integrationEventPublisher);
