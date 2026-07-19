using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using MediatR;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.AddGroupParticipants;

public sealed class AddGroupParticipantsCommandHandlerV2(
    IConversationMembershipWriteRepository membershipRepository,
    IConversationParticipantWriteRepository participantRepository,
    IChatMessageV2WriteRepository messageRepository,
    IUnitOfWork unitOfWork,
    ILocalEventDispatcher localEventDispatcher,
    IEnumerable<IAggregateBeforeSaveProcessor<AddGroupParticipantsCommandV2, ConversationMembership>> processors)
    : AggregateRootUpdateCommandHandlerBaseV3<
        AddGroupParticipantsCommandV2,
        Unit,
        ConversationMembership>(localEventDispatcher, unitOfWork, processors)
{
    protected override async Task<FlowChatResult<ConversationMembership?>> FetchAggregateRootAsync(
        AddGroupParticipantsCommandV2 request,
        CancellationToken cancellationToken)
    {
        var membership = await membershipRepository.GetByConversationIdAsync(
            Id<ConversationV2>.FromGuid(request.ConversationId),
            cancellationToken);

        return membership is null
            ? FlowChatResult<ConversationMembership?>.Failure(DomainError.NotFound("Conversation membership not found."))
            : FlowChatResult<ConversationMembership?>.Success(membership);
    }

    protected override async Task<FlowChatResult<Unit>> ExecuteAsync(
        AddGroupParticipantsCommandV2 request,
        CancellationToken cancellationToken)
    {
        if (AggregateRoot!.ConversationType != ConversationType.Group)
        {
            return FlowChatResult<Unit>.Failure(
                DomainError.BadRequest("Participants can only be added to group conversations."));
        }
        //Review2-4: Ta walidaja wygląda jakby mogla wyleciec do AddGroupParticipantsCommandValidatorV2 
        if (request.ParticipantUserIds.Count == 0 ||
            request.ParticipantUserIds.Any(x => x == Guid.Empty) ||
            request.ParticipantUserIds.Distinct().Count() != request.ParticipantUserIds.Count)
        {
            return FlowChatResult<Unit>.Failure(
                DomainError.BadRequest("Participant user ids must be non-empty and cannot contain duplicates."));
        }

        var conversationId = Id<ConversationV2>.FromGuid(request.ConversationId);
        var participantIds = request.ParticipantUserIds
            .Select(Id<UserProfileMarker>.FromGuid)
            .ToArray();
        var existing = await participantRepository.GetActiveByUserIdsAsync(
            conversationId,
            participantIds,
            cancellationToken);

        //Review2-5: Wydaje mi się że więcej sensu ma nie zwracanie failure a dodanie tych uczestników którzy jeszcze niesą dodanie do konwersacji
        if (existing.Count > 0)
        {
            return FlowChatResult<Unit>.Failure(
                DomainError.Conflict("At least one user is already an active participant."));
        }

        var maxSequence = await messageRepository.GetMaxSequenceNumAsync(
            conversationId,
            cancellationToken);
        AggregateRoot.AddParticipants(participantIds, maxSequence.GetValueOrDefault());
        SetUpdated();

        return FlowChatResult<Unit>.Success(Unit.Value);
    }
}
