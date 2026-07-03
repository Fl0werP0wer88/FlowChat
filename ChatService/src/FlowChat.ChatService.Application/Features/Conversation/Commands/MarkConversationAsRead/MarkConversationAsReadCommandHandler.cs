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
    IUnitOfWork unitOfWork,
    ILocalEventDispatcher domainEventDispatcher,
    IEnumerable<IAggregateBeforeSaveProcessor<MarkConversationAsReadCommand, ConversationAggregate>> beforeSaveProcessors)
    : AggregateRootUpdateCommandHandlerBaseV3<MarkConversationAsReadCommand, Unit, ConversationAggregate>(
        domainEventDispatcher,
        unitOfWork,
        beforeSaveProcessors)
{
    private ConversationAggregate? _conversation;

    protected override async Task<FlowChatResult<Unit>> ExecuteAsync(
        MarkConversationAsReadCommand request,
        CancellationToken cancellationToken)
    {
        _conversation = await conversationRepository.GetByIdAsync(request.ConversationId, cancellationToken);
        if (_conversation is null)
            return FlowChatResult<Unit>.Failure(DomainError.NotFound("Conversation not found."));

        var participantUserId = Id<UserProfileMarker>.FromGuid(request.ParticipantUserId);
        if (!_conversation.HasParticipant(participantUserId))
            return FlowChatResult<Unit>.Failure(DomainError.Unauthorized("Requesting user is not a participant of this conversation."));

        var wasUpdated = _conversation.MarkParticipantAsRead(participantUserId);
        if (wasUpdated)
        {
            SetUpdated();
        }

        return FlowChatResult<Unit>.Success(Unit.Value);
    }

    protected override ConversationAggregate GetAggregateRoot() =>
        _conversation ?? throw new InvalidOperationException("Aggregate root instance is not available.");
}
