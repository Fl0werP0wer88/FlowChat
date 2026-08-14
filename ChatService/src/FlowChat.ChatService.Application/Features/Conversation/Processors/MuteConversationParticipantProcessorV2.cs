using AutoMapper;
using FlowChat.ChatService.Application.Features.Conversation.Commands.MuteConversationParticipant;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Core.Messaging.ChatService.ReadModels;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;

namespace FlowChat.ChatService.Application.Features.Conversation.Processors;

public sealed class MuteConversationParticipantProcessorV2(
    IMapper mapper,
    IOutboxIntegrationEventPublisher publisher)
    : PublishProjectionIntegrationEventProcessorV2<
        MuteConversationParticipantCommandV2,
        ConversationParticipant,
        ConversationParticipantReadModelV2>(mapper, publisher);
