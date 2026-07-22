using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using MediatR;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.RemoveGroupParticipants;

public sealed class RemoveGroupParticipantsCommandHandlerV2(
    IConversationMembershipWriteRepository membershipRepository,
    IConversationParticipantWriteRepository participantRepository,
    IUnitOfWork unitOfWork,
    ILocalEventDispatcher localEventDispatcher,
    IEnumerable<IAggregateBeforeSaveProcessorV2<RemoveGroupParticipantsCommandV2, ConversationMembership>> processors)
    : AggregateRootUpdateCommandHandlerBaseV3<
        RemoveGroupParticipantsCommandV2,
        Unit,
        ConversationMembership>(localEventDispatcher, unitOfWork, processors)
{
    protected override async Task<FlowChatResult<ConversationMembership?>> FetchAggregateRootAsync(
        RemoveGroupParticipantsCommandV2 request,
        CancellationToken cancellationToken)
    {
        var membership = await membershipRepository.GetByConversationIdAsync(
            Id<ConversationV2>.FromGuid(request.ConversationId),
            cancellationToken);

        return membership is null
            ? FlowChatResult<ConversationMembership?>.Failure(DomainError.NotFound("Conversation membership not found."))
            : FlowChatResult<ConversationMembership?>.Success(membership);
    }

    protected override async Task<FlowChatResult<AggregateMutation<Unit>>> ExecuteAsync(
        RemoveGroupParticipantsCommandV2 request,
        CancellationToken cancellationToken)
    {
        if (AggregateRoot!.ConversationType != ConversationType.Group)
        {
            return Failure(
                DomainError.BadRequest("Participants can only be removed from group conversations."));
        }

        var conversationId = Id<ConversationV2>.FromGuid(request.ConversationId);
        var participantIds = request.ParticipantUserIds
            .Select(Id<UserProfileMarker>.FromGuid)
            .ToArray();
        var existing = await participantRepository.GetActiveByUserIdsAsync(
            conversationId,
            participantIds,
            cancellationToken);
        var existingUserIds = existing
            .Select(participant => participant.UserId)
            .ToHashSet();
        var participantIdsToRemove = participantIds
            .Where(existingUserIds.Contains)
            .ToArray();

        if (participantIdsToRemove.Length == 0)
        {
            return Unchanged(Unit.Value);
        }

        if (AggregateRoot.ParticipantCount - participantIdsToRemove.Length < 2)
        {
            return Failure(
                DomainError.BadRequest("Group conversations must have at least two participants."));
        }

        AggregateRoot.RemoveParticipants(participantIdsToRemove);

        return Updated(Unit.Value);
    }
}
