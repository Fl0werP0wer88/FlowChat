using FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Domain.Notifications;
using MediatR;

namespace FlowChat.RealtimeService.Application.Messages.Commands.ReceiveMessage;

public sealed class ReceiveMessageCommandHandler(IRealtimeClientDispatcher realtimeClientDispatcher)
    : IRequestHandler<ReceiveMessageCommand>
{
    private readonly IRealtimeClientDispatcher _realtimeClientDispatcher = realtimeClientDispatcher
        ?? throw new ArgumentNullException(nameof(realtimeClientDispatcher));

    public Task Handle(ReceiveMessageCommand request, CancellationToken cancellationToken)
    {
        if (request.MessageId == Guid.Empty)
        {
            throw new InvalidOperationException("MessageId is required.");
        }

        if (request.ConversationId == Guid.Empty)
        {
            throw new InvalidOperationException("ConversationId is required.");
        }

        if (request.SenderUserId == Guid.Empty)
        {
            throw new InvalidOperationException("SenderUserId is required.");
        }

        if (string.IsNullOrWhiteSpace(request.SenderDisplayName))
        {
            throw new InvalidOperationException("SenderDisplayName is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Text))
        {
            throw new InvalidOperationException("Text is required.");
        }

        var recipientUserIds = NormalizeRecipientUserIds(request.RecipientUserIds);
        var notification = new ChatMessageNotification(
            request.MessageId,
            request.ConversationId,
            request.SenderUserId,
            request.SenderDisplayName.Trim(),
            request.Text.Trim(),
            request.SentAtUtc,
            recipientUserIds);

        return _realtimeClientDispatcher.ReceiveMessageAsync(notification, cancellationToken);
    }

    private static Guid[] NormalizeRecipientUserIds(IReadOnlyCollection<Guid> recipientUserIds)
    {
        var normalized = recipientUserIds
            .Where(userId => userId != Guid.Empty)
            .Distinct()
            .ToArray();

        if (normalized.Length == 0)
        {
            throw new InvalidOperationException("RecipientUserIds must contain at least one valid user id.");
        }

        return normalized;
    }
}
