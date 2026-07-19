using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using MediatR;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.HideConversationParticipant;

public sealed class HideConversationParticipantCommandHandlerV2(
    IConversationParticipantWriteRepository repository,
    IUnitOfWork unitOfWork,
    ILocalEventDispatcher dispatcher,
    IEnumerable<IAggregateBeforeSaveProcessor<HideConversationParticipantCommandV2, ConversationParticipant>> processors)
    : AggregateRootUpdateCommandHandlerBaseV3<HideConversationParticipantCommandV2, Unit, ConversationParticipant>(
        dispatcher, unitOfWork, processors)
{
    protected override async Task<FlowChatResult<ConversationParticipant?>> FetchAggregateRootAsync(
        HideConversationParticipantCommandV2 request,
        CancellationToken cancellationToken)
    {
        var participant = await repository.GetActiveAsync(
            Id<ConversationV2>.FromGuid(request.ConversationId),
            Id<UserProfileMarker>.FromGuid(request.RequestingUserId),
            cancellationToken);
        return participant is null
            ? FlowChatResult<ConversationParticipant?>.Failure(DomainError.NotFound("Conversation participant not found."))
            : FlowChatResult<ConversationParticipant?>.Success(participant);
    }

    protected override Task<FlowChatResult<Unit>> ExecuteAsync(
        HideConversationParticipantCommandV2 request,
        CancellationToken cancellationToken)
    {
        if (AggregateRoot!.Hide()) SetUpdated();
        return Task.FromResult(FlowChatResult<Unit>.Success(Unit.Value));
    }
}
