using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using MediatR;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;
using ConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.Conversation;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.MarkConversationAsRead;

public sealed class MarkConversationAsReadCommandHandler(
    IConversationWriteRepository conversationRepository,
    IChatMessageReadRepository chatMessageRepository,
    IUnitOfWork unitOfWork,
    ILocalEventDispatcher domainEventDispatcher,
    IEnumerable<IAggregateBeforeSaveProcessor<MarkConversationAsReadCommand, ConversationAggregate>> beforeSaveProcessors)
    : AggregateRootUpdateCommandHandlerBaseV3<MarkConversationAsReadCommand, Unit, ConversationAggregate>(
        domainEventDispatcher,
        unitOfWork,
        beforeSaveProcessors)
{
    protected override async Task<FlowChatResult<ConversationAggregate?>> FetchAggregateRootAsync(
        MarkConversationAsReadCommand request,
        CancellationToken cancellationToken)
    {
        var conversation = await conversationRepository.GetByIdAsync(request.ConversationId, cancellationToken);
        if (conversation is null)
            return FlowChatResult<ConversationAggregate?>.Failure(DomainError.NotFound("Conversation not found."));

        return FlowChatResult<ConversationAggregate?>.Success(conversation);
    }

    protected override async Task<FlowChatResult<Unit>> ExecuteAsync(
        MarkConversationAsReadCommand request,
        CancellationToken cancellationToken)
    {
        var participantUserId = Id<UserProfileMarker>.FromGuid(request.ParticipantUserId);
        if (!AggregateRoot!.HasParticipant(participantUserId))
            return FlowChatResult<Unit>.Failure(DomainError.Unauthorized("Requesting user is not a participant of this conversation."));

        var maxSequenceNum = await chatMessageRepository.GetMaxSequenceNumAsync(
            request.ConversationId,
            cancellationToken);
        var wasUpdated = AggregateRoot.MarkParticipantAsRead(
            participantUserId,
            maxSequenceNum.GetValueOrDefault());
        if (wasUpdated)
        {
            SetUpdated();
        }

        return FlowChatResult<Unit>.Success(Unit.Value);
    }
}
