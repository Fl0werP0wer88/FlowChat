using CSharpFunctionalExtensions;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Application.Contracts.Persistence;
using FlowChat.RealtimeService.Domain.Enums;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using MediatR;

namespace FlowChat.RealtimeService.Application.Features.Message.Commands.RouteMessage;

public sealed class RouteMessageCommandHandler(
    IRealtimeEventRouter realtimeEventRouter,
    IChatServiceInternalApiClient chatServiceInternalApiClient,
    IRealtimeGroupMembershipRevisionTrackerRepository realtimeGroupMembershipRevisionTrackerRepository,
    IRealtimeGroupMembershipReadModelRepository realtimeGroupMembershipReadModelRepository)
    : ICommandHandler<RouteMessageCommand, Unit>
{
    private readonly IRealtimeEventRouter _realtimeEventRouter = realtimeEventRouter
        ?? throw new ArgumentNullException(nameof(realtimeEventRouter));
    private readonly IChatServiceInternalApiClient _chatServiceInternalApiClient = chatServiceInternalApiClient
        ?? throw new ArgumentNullException(nameof(chatServiceInternalApiClient));
    private readonly IRealtimeGroupMembershipRevisionTrackerRepository _realtimeGroupMembershipRevisionTrackerRepository = realtimeGroupMembershipRevisionTrackerRepository
        ?? throw new ArgumentNullException(nameof(realtimeGroupMembershipRevisionTrackerRepository));
    private readonly IRealtimeGroupMembershipReadModelRepository _realtimeGroupMembershipReadModelRepository = realtimeGroupMembershipReadModelRepository
        ?? throw new ArgumentNullException(nameof(realtimeGroupMembershipReadModelRepository));

    public async Task<FlowChatResult<Unit>> Handle(RouteMessageCommand request, CancellationToken cancellationToken)
    {
        var trackedRevision = await _realtimeGroupMembershipRevisionTrackerRepository.GetRevisionAsync(
            request.ConversationId,
            cancellationToken);

        // Membership projection has not yet caught up with the conversation state this message was sent against;
        // retrying lets the pending GroupConversationParticipants/Duet event catch up before we resolve recipients.
        if (request.ConversationMembershipRevision > (trackedRevision ?? 0))
        {
            return FlowChatResult<Unit>.Failure(
                DomainError.UnExpected(
                    $"Conversation {request.ConversationId} membership projection revision {trackedRevision ?? 0} is behind message revision {request.ConversationMembershipRevision}.",
                    FailureKind.Transient));
        }

        var conversationMemberUserIds = await _realtimeGroupMembershipReadModelRepository.GetUserIdsByResourceIdAsync(
            RealtimeGroupType.Conversation,
            request.ConversationId,
            cancellationToken);
        var recipientUserIds = conversationMemberUserIds
            .Where(userId => userId != Guid.Empty && userId != request.SenderUserId)
            .Distinct()
            .ToArray();

        // The version check above already guarantees the membership projection is caught up with this message,
        // so an empty result here means a real data problem, not a race — surface it instead of routing to nobody.
        if (recipientUserIds.Length == 0)
        {
            return FlowChatResult<Unit>.Failure(
                DomainError.UnExpected(
                    $"Conversation {request.ConversationId} has no known recipients other than the sender at membership revision {trackedRevision}."));
        }

        //ToDo: Rozważyć przesylanie tego kafką
        var sequenceNum = await _chatServiceInternalApiClient.SetChatMessageSequenceNumberAsync(
            request.MessageId,
            request.ConversationId,
            cancellationToken);
        var deliveredAtUtc = DateTimeOffset.UtcNow;

        var notification = new ChatMessageParam(
            request.MessageId,
            request.ConversationId,
            request.SenderUserId,
            request.Text!.Trim(),
            sequenceNum,
            request.SentAtUtc,
            deliveredAtUtc,
            recipientUserIds);

        // TODO: Add a Realtime inbox before treating SignalR dispatch retries as safe
        await _realtimeEventRouter.RouteMessageAsync(notification, cancellationToken);

        // TODO: Add ChatService idempotency storage before treating delivery retries as safe
        await _chatServiceInternalApiClient.MarkChatMessageAsDeliveredAsync(
            request.MessageId,
            request.ConversationId,
            deliveredAtUtc,
            cancellationToken);

        return FlowChatResult<Unit>.Success(Unit.Value);
    }
}
