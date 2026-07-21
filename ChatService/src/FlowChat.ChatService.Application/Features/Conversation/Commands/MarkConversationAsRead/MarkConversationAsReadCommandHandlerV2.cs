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
    IChatMessageV2WriteRepository messageRepository,
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
        var mutationType = FlowChat.Shared.Domain.MutationType.Unchanged;
        var maxSequence = await messageRepository.GetMaxSequenceNumAsync(
            Id<ConversationV2>.FromGuid(request.ConversationId),
            cancellationToken);

        if (AggregateRoot!.AdvanceReadCursor(maxSequence.GetValueOrDefault()))
        {
            mutationType = FlowChat.Shared.Domain.MutationType.Updated;
        }

        return Mutation(mutationType, Unit.Value);
    }
}
