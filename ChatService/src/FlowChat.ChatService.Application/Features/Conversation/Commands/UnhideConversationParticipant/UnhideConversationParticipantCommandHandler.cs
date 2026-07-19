using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using MediatR;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;
using DuetConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.DuetConversation;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.UnhideConversationParticipant;

public sealed class UnhideConversationParticipantCommandHandler(
    IDuetConversationWriteRepository duetConversationRepository,
    IUnitOfWork unitOfWork,
    ILocalEventDispatcher domainEventDispatcher,
    IEnumerable<IAggregateBeforeSaveProcessor<UnhideConversationParticipantCommand, DuetConversationAggregate>> beforeSaveProcessors)
    : AggregateRootUpdateCommandHandlerBaseV3<UnhideConversationParticipantCommand, Unit, DuetConversationAggregate>(
        domainEventDispatcher,
        unitOfWork,
        beforeSaveProcessors)
{
    protected override async Task<FlowChatResult<DuetConversationAggregate?>> FetchAggregateRootAsync(
        UnhideConversationParticipantCommand request,
        CancellationToken cancellationToken)
    {
        var conversation = await duetConversationRepository.GetByIdAsync(request.ConversationId, cancellationToken);
        if (conversation is null)
            return FlowChatResult<DuetConversationAggregate?>.Failure(DomainError.NotFound("Conversation not found."));

        return FlowChatResult<DuetConversationAggregate?>.Success(conversation);
    }

    protected override Task<FlowChatResult<AggregateMutation<Unit>>> ExecuteAsync(
        UnhideConversationParticipantCommand request,
        CancellationToken cancellationToken)
    {
        var mutationType = FlowChat.Shared.Domain.MutationType.Unchanged;
        var requestingUserId = Id<UserProfileMarker>.FromGuid(request.RequestingUserId);
        if (!AggregateRoot!.HasParticipant(requestingUserId))
            return Task.FromResult(Failure(DomainError.Unauthorized("Requesting user is not a participant of this conversation.")));

        if (AggregateRoot.UnhideParticipant(requestingUserId))
        {
            mutationType = FlowChat.Shared.Domain.MutationType.Updated;
        }

        return Task.FromResult(Mutation(mutationType, Unit.Value));
    }
}
