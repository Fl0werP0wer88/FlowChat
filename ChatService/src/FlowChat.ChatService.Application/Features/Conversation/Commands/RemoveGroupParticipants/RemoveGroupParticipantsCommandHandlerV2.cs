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
    IEnumerable<IAggregateBeforeSaveProcessor<RemoveGroupParticipantsCommandV2, ConversationMembership>> processors)
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
        var conversationId = Id<ConversationV2>.FromGuid(request.ConversationId);
        var participantIds = request.ParticipantUserIds
            .Select(Id<UserProfileMarker>.FromGuid)
            .ToArray();
        var existing = await participantRepository.GetActiveByUserIdsAsync(
            conversationId,
            participantIds,
            cancellationToken);
        //Review2-7: Wydaje mi się że więcej sensu ma nie zwracanie failure, a usunięcie tych uczestników którzy są w konwersacji. Oceń pomysł
        if (existing.Count != participantIds.Length)
        {
            return Failure(
                DomainError.NotFound("At least one participant was not found."));
        }

        if (AggregateRoot!.ConversationType != ConversationType.Group ||
            AggregateRoot.ParticipantCount - participantIds.Length < 2)
        {
            return Failure(
                DomainError.BadRequest("Group conversations must have at least two participants."));
        }

        AggregateRoot.RemoveParticipants(participantIds);

        return Updated(Unit.Value);
    }
}
