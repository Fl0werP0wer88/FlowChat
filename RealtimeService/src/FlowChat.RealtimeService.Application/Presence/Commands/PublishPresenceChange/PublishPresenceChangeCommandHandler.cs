using FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Domain.Notifications;
using MediatR;

namespace FlowChat.RealtimeService.Application.Presence.Commands.PublishPresenceChange;

public sealed class PublishPresenceChangeCommandHandler(IRealtimeClientDispatcher realtimeClientDispatcher)
    : IRequestHandler<PublishPresenceChangeCommand>
{
    private static readonly HashSet<string> AllowedStatuses =
        ["online", "away", "offline"];

    private readonly IRealtimeClientDispatcher _realtimeClientDispatcher = realtimeClientDispatcher
        ?? throw new ArgumentNullException(nameof(realtimeClientDispatcher));

    public Task Handle(PublishPresenceChangeCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty)
        {
            throw new InvalidOperationException("UserId is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Status))
        {
            throw new InvalidOperationException("Status is required.");
        }

        var normalizedStatus = request.Status.Trim().ToLowerInvariant();
        if (!AllowedStatuses.Contains(normalizedStatus))
        {
            throw new InvalidOperationException("Status must be one of: online, away, offline.");
        }

        var recipientUserIds = NormalizeRecipientUserIds(request.RecipientUserIds);
        var notification = new PresenceChangedNotification(
            request.UserId,
            normalizedStatus,
            request.ChangedAtUtc,
            recipientUserIds);

        return _realtimeClientDispatcher.PresenceChangedAsync(notification, cancellationToken);
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
