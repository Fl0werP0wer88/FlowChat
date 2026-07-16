using AutoMapper;
using FlowChat.Core.Messaging.ChatService.ReadModels;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors.Interfaces;
using GroupConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.GroupConversation;
using ParticipantUser = FlowChat.ChatService.Domain.Entities.Conversation.ParticipantUser;

namespace FlowChat.ChatService.Application.Features.Conversation.Processors;

public sealed class GroupConversationMembershipProjectionProcessor<TCommand>(
    IMapper mapper,
    IOutboxIntegrationEventPublisher integrationEventPublisher,
    IDeltaProjectionRevisionProvider<GroupConversationAggregate, GroupConversationMembershipReadModel> revisionProvider)
    : PublishDeltaProjectionIntegrationEventProcessor<
        TCommand,
        GroupConversationAggregate,
        ParticipantUser,
        GroupConversationMembershipReadModel>(mapper, integrationEventPublisher, revisionProvider);
