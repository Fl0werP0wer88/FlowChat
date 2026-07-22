using AutoMapper;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Domain.Entities.Conversation.Events;
using FlowChat.Core.Messaging.ChatService.ReadModels;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.BatchAggregateCommandHandlerBase.BeforeSaveProcessors;

namespace FlowChat.ChatService.Application.Features.Conversation.Processors;

public sealed class AddConversationMembershipDeltaProcessorV2
    : AggregateBeforeSaveDeltaProcessorV2<
        ConversationParticipantsAddedDomainEventV2,
        ConversationParticipant,
        ConversationMembershipReadModelV2>
{
    public AddConversationMembershipDeltaProcessorV2(
        IMapper mapper,
        IOutboxIntegrationEventPublisher integrationEventPublisher,
        IAggregateDeltaProjectionMetadataProviderV2<
            ConversationParticipantsAddedDomainEventV2,
            ConversationParticipant> metadataProvider)
        : base(mapper, integrationEventPublisher, metadataProvider)
    {
    }
}
