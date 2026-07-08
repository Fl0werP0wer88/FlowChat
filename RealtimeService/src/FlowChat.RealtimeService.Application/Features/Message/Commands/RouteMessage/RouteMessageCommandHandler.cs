using CSharpFunctionalExtensions;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.RealtimeService.Application.Features.Message.Commands.RouteMessage;

public sealed class RouteMessageCommandHandler(
    IRealtimeEventRouter realtimeEventRouter,
    IChatServiceInternalApiClient chatServiceInternalApiClient)
    : ICommandHandler<RouteMessageCommand, Unit>
{
    private readonly IRealtimeEventRouter _realtimeEventRouter = realtimeEventRouter
        ?? throw new ArgumentNullException(nameof(realtimeEventRouter));
    private readonly IChatServiceInternalApiClient _chatServiceInternalApiClient = chatServiceInternalApiClient
        ?? throw new ArgumentNullException(nameof(chatServiceInternalApiClient));

    public async Task<FlowChatResult<Unit>> Handle(RouteMessageCommand request, CancellationToken cancellationToken)
    {
        var recipientUserIds = NormalizeRecipientUserIds(request.RecipientUserIds);

        //ToDo: Rozważyć przesylanie tego kafką,
        var sequenceNum = await _chatServiceInternalApiClient.SetChatMessageSequenceNumberAsync(
            request.MessageId,
            request.ConversationId,
            cancellationToken);
        var deliveredAtUtc = DateTimeOffset.UtcNow;

        var notification = new ChatMessageParam(
            request.MessageId,
            request.ConversationId,
            request.SenderUserId,
            request.SenderDisplayName!.Trim(),
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
