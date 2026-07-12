using CSharpFunctionalExtensions;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Application.Contracts.Persistence;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using MediatR;

namespace FlowChat.RealtimeService.Application.Features.Message.Commands.RouteMessage;

public sealed class RouteMessageCommandHandler(
    IRealtimeEventRouter realtimeEventRouter,
    IChatServiceInternalApiClient chatServiceInternalApiClient,
    IRealtimeGroupMembershipVersionTrackerRepository realtimeGroupMembershipVersionTrackerRepository)
    : ICommandHandler<RouteMessageCommand, Unit>
{
    private readonly IRealtimeEventRouter _realtimeEventRouter = realtimeEventRouter
        ?? throw new ArgumentNullException(nameof(realtimeEventRouter));
    private readonly IChatServiceInternalApiClient _chatServiceInternalApiClient = chatServiceInternalApiClient
        ?? throw new ArgumentNullException(nameof(chatServiceInternalApiClient));
    private readonly IRealtimeGroupMembershipVersionTrackerRepository _realtimeGroupMembershipVersionTrackerRepository = realtimeGroupMembershipVersionTrackerRepository
        ?? throw new ArgumentNullException(nameof(realtimeGroupMembershipVersionTrackerRepository));

    public async Task<FlowChatResult<Unit>> Handle(RouteMessageCommand request, CancellationToken cancellationToken)
    {
        var recipientUserIds = NormalizeRecipientUserIds(request.RecipientUserIds);

        var trackedVersion = await _realtimeGroupMembershipVersionTrackerRepository.GetVersionAsync(
            request.ConversationId,
            cancellationToken);

        // Membership projection has not yet caught up with the conversation state this message was sent against;
        // retrying lets the pending GroupConversationParticipants/Duet event catch up before we resolve recipients.
        if (request.ConversationVersionAtSend > (trackedVersion ?? 0))
        {
            return FlowChatResult<Unit>.Failure(
                DomainError.UnExpected(
                    $"Conversation {request.ConversationId} membership projection version {trackedVersion ?? 0} is behind message version {request.ConversationVersionAtSend}.",
                    FailureKind.Transient));
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

    private static Guid[] NormalizeRecipientUserIds(IReadOnlyCollection<Guid> recipientUserIds) =>
        recipientUserIds
            .Where(userId => userId != Guid.Empty)
            .Distinct()
            .ToArray();
}
