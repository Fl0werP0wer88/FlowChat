using AutoMapper;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Domain.Entities.Conversation.Events;
using FlowChat.Core.Messaging.ChatService.ReadModels;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.BatchAggregateCommandHandlerBase.BeforeSaveProcessors;

namespace FlowChat.ChatService.Application.Features.Conversation.Processors;

public sealed class RemoveConversationMembershipDeltaProcessorV2
    : AggregateBeforeSaveDeltaProcessorV2<
        ConversationParticipantsRemovedDomainEventV2,
        ConversationParticipant,
        ConversationMembershipReadModelV2>
{
    public RemoveConversationMembershipDeltaProcessorV2(
        IMapper mapper,
        IOutboxIntegrationEventPublisher integrationEventPublisher,
        IAggregateDeltaProjectionKeyProviderV2<
            ConversationParticipantsRemovedDomainEventV2,
            ConversationParticipant> keyProvider)
        : base(mapper, integrationEventPublisher, keyProvider)
    {
    }
}
