using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using MediatR;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.MarkConversationAsRead;

public sealed class MarkConversationAsReadCommandHandlerV2(
    IConversationParticipantWriteRepository participantRepository,
    IConversationMessageSequenceReadRepository sequenceReadRepository,
    IUnitOfWork unitOfWork,
    ILocalEventDispatcher localEventDispatcher,
    IEnumerable<IAggregateBeforeSaveProcessorV2<MarkConversationAsReadCommandV2, ConversationParticipant>> processors)
    : AggregateRootUpdateCommandHandlerBaseV3<
        MarkConversationAsReadCommandV2,
        Unit,
        ConversationParticipant>(localEventDispatcher, unitOfWork, processors)
{
    protected override async Task<FlowChatResult<ConversationParticipant?>> FetchAggregateRootAsync(
        MarkConversationAsReadCommandV2 request,
        CancellationToken cancellationToken)
    {
        var participant = await participantRepository.GetActiveAsync(
            Id<ConversationV2>.FromGuid(request.ConversationId),
            Id<UserProfileMarker>.FromGuid(request.ParticipantUserId),
            cancellationToken);

        return participant is null
            ? FlowChatResult<ConversationParticipant?>.Failure(DomainError.NotFound("Conversation participant not found."))
            : FlowChatResult<ConversationParticipant?>.Success(participant);
    }

    protected override async Task<FlowChatResult<AggregateMutation<Unit>>> ExecuteAsync(
        MarkConversationAsReadCommandV2 request,
        CancellationToken cancellationToken)
    {
        var currentSequenceNum = await sequenceReadRepository.GetCurrentAsync(
            request.ConversationId,
            cancellationToken) ?? 0;

        if (request.SequenceNum > currentSequenceNum)
        {
            return Failure(DomainError.BadRequest(
                "SequenceNum cannot exceed the current conversation sequence."));
        }

        return AggregateRoot!.AdvanceReadCursor(request.SequenceNum)
            ? Updated(Unit.Value)
            : Unchanged(Unit.Value);
    }
}
