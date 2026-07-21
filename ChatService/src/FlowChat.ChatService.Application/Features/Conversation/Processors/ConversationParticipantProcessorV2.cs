using AutoMapper;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Core.Messaging.ChatService.ReadModels;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;

namespace FlowChat.ChatService.Application.Features.Conversation.Processors;

public sealed class ConversationParticipantProcessorV2<TTrigger>(
    IMapper mapper,
    IOutboxIntegrationEventPublisher publisher)
    : PublishProjectionIntegrationEventProcessorV2<
        TTrigger,
        ConversationParticipant,
        ConversationParticipantReadModelV2>(mapper, publisher);
