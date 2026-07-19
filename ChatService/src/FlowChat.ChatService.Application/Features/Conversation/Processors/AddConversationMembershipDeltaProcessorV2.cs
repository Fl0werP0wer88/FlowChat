using FlowChat.ChatService.Application.Features.Conversation.Commands.AddGroupParticipants;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Core.Messaging;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Application.Features.Conversation.Processors;

public sealed class AddConversationMembershipDeltaProcessorV2(
    ConversationMembershipDeltaPublisherV2 publisher)
    : IAggregateBeforeSaveProcessor<AddGroupParticipantsCommandV2, ConversationMembership>
{
    public void CaptureBeforeState(ConversationMembership aggregate)
    {
    }

    public Task ProcessAsync(
        AddGroupParticipantsCommandV2 command,
        ConversationMembership aggregate,
        MutationType mutationType,
        CancellationToken cancellationToken)
    {
        if (mutationType != MutationType.Updated)
            throw new InvalidOperationException("Membership add processor requires an updated aggregate.");

        return publisher.PublishAsync(
            aggregate,
            command.ParticipantUserIds.Select(Id<UserProfileMarker>.FromGuid),
            DeltaOperationType.Added,
            cancellationToken);
    }
}
