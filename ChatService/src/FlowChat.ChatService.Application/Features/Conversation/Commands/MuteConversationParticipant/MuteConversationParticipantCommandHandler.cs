using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using MediatR;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;
using DuetConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.DuetConversation;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.MuteConversationParticipant;

public sealed class MuteConversationParticipantCommandHandler(
    IDuetConversationWriteRepository duetConversationRepository,
    IUnitOfWork unitOfWork,
    ILocalEventDispatcher domainEventDispatcher,
    IEnumerable<IAggregateBeforeSaveProcessor<MuteConversationParticipantCommand, DuetConversationAggregate>> beforeSaveProcessors)
    : AggregateRootUpdateCommandHandlerBaseV3<MuteConversationParticipantCommand, Unit, DuetConversationAggregate>(
        domainEventDispatcher,
        unitOfWork,
        beforeSaveProcessors)
{
    protected override async Task<FlowChatResult<DuetConversationAggregate?>> FetchAggregateRootAsync(
        MuteConversationParticipantCommand request,
        CancellationToken cancellationToken)
    {
        var conversation = await duetConversationRepository.GetByIdAsync(request.ConversationId, cancellationToken);
        if (conversation is null)
            return FlowChatResult<DuetConversationAggregate?>.Failure(DomainError.NotFound("Conversation not found."));

        return FlowChatResult<DuetConversationAggregate?>.Success(conversation);
    }

    protected override Task<FlowChatResult<Unit>> ExecuteAsync(
        MuteConversationParticipantCommand request,
        CancellationToken cancellationToken)
    {
        var requestingUserId = Id<UserProfileMarker>.FromGuid(request.RequestingUserId);
        if (!AggregateRoot!.HasParticipant(requestingUserId))
            return Task.FromResult(FlowChatResult<Unit>.Failure(DomainError.Unauthorized("Requesting user is not a participant of this conversation.")));

        if (AggregateRoot.MuteParticipant(requestingUserId))
        {
            SetUpdated();
        }

        return Task.FromResult(FlowChatResult<Unit>.Success(Unit.Value));
    }
}
