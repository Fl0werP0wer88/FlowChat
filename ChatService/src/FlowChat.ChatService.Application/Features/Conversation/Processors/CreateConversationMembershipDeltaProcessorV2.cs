using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Domain.Entities.Conversation.Events;
using FlowChat.Core.Messaging;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;

namespace FlowChat.ChatService.Application.Features.Conversation.Processors;

public sealed class CreateConversationMembershipDeltaProcessorV2(
    ConversationMembershipDeltaPublisherV2 publisher)
    : IAggregateBeforeSaveProcessorV2<ConversationCreatedDomainEventV2, ConversationMembership>
{
    public Task ProcessAsync(
        ConversationCreatedDomainEventV2 command,
        ConversationMembership aggregate,
        MutationType mutationType,
        CancellationToken cancellationToken)
    {
        if (mutationType != MutationType.Created)
            throw new InvalidOperationException("Membership create processor requires a created aggregate.");

        return publisher.PublishAsync(
            aggregate,
            command.ParticipantUserIds,
            DeltaOperationType.Added,
            cancellationToken);
    }
}
