using CSharpFunctionalExtensions;
using FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using MediatR;

namespace FlowChat.RealtimeService.Application.Features.Message.Commands.RouteMessage;

public sealed class RouteMessageCommandHandler(IRealtimeEventRouter realtimeEventRouter)
    : ICommandHandler<RouteMessageCommand, Unit>
{
    private readonly IRealtimeEventRouter _realtimeEventRouter = realtimeEventRouter
        ?? throw new ArgumentNullException(nameof(realtimeEventRouter));

    public async Task<FlowChatResult<Unit>> Handle(RouteMessageCommand request, CancellationToken cancellationToken)
    {
        if (request.MessageId == Guid.Empty)
        {
            return FlowChatResult<Unit>.Failure(DomainError.BadRequest("MessageId is required."));
        }

        if (request.ConversationId == Guid.Empty)
        {
            return FlowChatResult<Unit>.Failure(DomainError.BadRequest("ConversationId is required."));
        }

        if (request.SenderUserId == Guid.Empty)
        {
            return FlowChatResult<Unit>.Failure(DomainError.BadRequest("SenderUserId is required."));
        }

        if (string.IsNullOrWhiteSpace(request.SenderDisplayName))
        {
            return FlowChatResult<Unit>.Failure(DomainError.BadRequest("SenderDisplayName is required."));
        }

        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return FlowChatResult<Unit>.Failure(DomainError.BadRequest("Text is required."));
        }

        var recipientUserIds = NormalizeRecipientUserIds(request.RecipientUserIds);
        if (recipientUserIds.Length == 0)
        {
            return FlowChatResult<Unit>.Failure(
                DomainError.BadRequest("RecipientUserIds must contain at least one valid user id."));
        }

        var notification = new ChatMessageNotification(
            request.MessageId,
            request.ConversationId,
            request.SenderUserId,
            request.SenderDisplayName.Trim(),
            request.Text.Trim(),
            request.SentAtUtc,
            recipientUserIds);

        await _realtimeEventRouter.RouteMessageAsync(notification, cancellationToken);

        return FlowChatResult<Unit>.Success(Unit.Value);
    }

    private static Guid[] NormalizeRecipientUserIds(IReadOnlyCollection<Guid> recipientUserIds) =>
        recipientUserIds
            .Where(userId => userId != Guid.Empty)
            .Distinct()
            .ToArray();
}
