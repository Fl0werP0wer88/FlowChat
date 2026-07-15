using AutoMapper;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.ChatService.ReadModels;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using DuetConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.DuetConversation;

namespace FlowChat.ChatService.Application.Features.Conversation.Processors;

public sealed class DuetConversationContactStateProjectionProcessor<TCommand>(
    IMapper mapper,
    IOutboxIntegrationEventPublisher integrationEventPublisher)
    : PublishProjectionIntegrationEventProcessor<TCommand, DuetConversationAggregate, DuetConversationContactStateReadModel>(
        mapper,
        integrationEventPublisher);
