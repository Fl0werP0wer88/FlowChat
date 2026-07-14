using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using MediatR;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;
using DuetConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.DuetConversation;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.BlockConversationParticipant;

public sealed class BlockConversationParticipantCommandHandler(
    IDuetConversationWriteRepository duetConversationRepository,
    IUnitOfWork unitOfWork,
    ILocalEventDispatcher domainEventDispatcher,
    IEnumerable<IAggregateBeforeSaveProcessor<BlockConversationParticipantCommand, DuetConversationAggregate>> beforeSaveProcessors)
    : AggregateRootUpdateCommandHandlerBaseV3<BlockConversationParticipantCommand, Unit, DuetConversationAggregate>(
        domainEventDispatcher,
        unitOfWork,
        beforeSaveProcessors)
{
    protected override async Task<FlowChatResult<DuetConversationAggregate?>> FetchAggregateRootAsync(
        BlockConversationParticipantCommand request,
        CancellationToken cancellationToken)
    {
        var conversation = await duetConversationRepository.GetByIdAsync(request.ConversationId, cancellationToken);
        if (conversation is null)
            return FlowChatResult<DuetConversationAggregate?>.Failure(DomainError.NotFound("Conversation not found."));

        return FlowChatResult<DuetConversationAggregate?>.Success(conversation);
    }

    protected override Task<FlowChatResult<Unit>> ExecuteAsync(
        BlockConversationParticipantCommand request,
        CancellationToken cancellationToken)
    {
        var requestingUserId = Id<UserProfileMarker>.FromGuid(request.RequestingUserId);
        var participant = AggregateRoot!.GetParticipant(requestingUserId);
        if (participant is null)
            return Task.FromResult(FlowChatResult<Unit>.Failure(DomainError.Unauthorized("Requesting user is not a participant of this conversation.")));

        if (participant.IsBlocked)
            return Task.FromResult(FlowChatResult<Unit>.Failure(DomainError.Conflict("Participant is already blocked.")));

        AggregateRoot.BlockParticipant(requestingUserId);
        SetUpdated();

        return Task.FromResult(FlowChatResult<Unit>.Success(Unit.Value));
    }
}
