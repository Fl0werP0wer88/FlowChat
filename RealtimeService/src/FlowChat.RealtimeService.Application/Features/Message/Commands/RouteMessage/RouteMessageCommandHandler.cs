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
        var deliveredAtUtc = DateTimeOffset.UtcNow;

        var notification = new ChatMessageParam(
            request.MessageId,
            request.ConversationId,
            request.SenderUserId,
            request.SenderDisplayName!.Trim(),
            request.Text!.Trim(),
            request.SentAtUtc,
            deliveredAtUtc,
            recipientUserIds);

        // ToDo: Maybe parallelize this with cancellation or compensation if RouteMessageAsync fails.
        await _realtimeEventRouter.RouteMessageAsync(notification, cancellationToken);
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
