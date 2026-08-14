using AutoMapper;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Domain.Entities.Conversation.Events;
using FlowChat.Core.Messaging.ChatService.ReadModels;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;

namespace FlowChat.ChatService.Application.Features.Conversation.Processors;

public sealed class ConversationParticipantsAddedProcessorV2(
    IMapper mapper,
    IOutboxIntegrationEventPublisher publisher)
    : PublishProjectionIntegrationEventProcessorV2<
        ConversationParticipantsAddedDomainEventV2,
        ConversationParticipant,
        ConversationParticipantReadModelV2>(mapper, publisher);
