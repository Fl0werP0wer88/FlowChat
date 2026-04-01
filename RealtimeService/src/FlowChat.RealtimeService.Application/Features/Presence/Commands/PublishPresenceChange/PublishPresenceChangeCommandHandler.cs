using CSharpFunctionalExtensions;
using FlowChat.Shared.Application;
using FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;
using FlowChat.Shared.Domain;
using FlowChat.RealtimeService.Domain.Notifications;
using MediatR;

namespace FlowChat.RealtimeService.Application.Features.Presence.Commands.PublishPresenceChange;

public sealed class PublishPresenceChangeCommandHandler(IRealtimeClientDispatcher realtimeClientDispatcher)
    : ICommandHandler<PublishPresenceChangeCommand, Unit>
{
    private static readonly HashSet<string> AllowedStatuses =
        ["online", "away", "offline"];

    private readonly IRealtimeClientDispatcher _realtimeClientDispatcher = realtimeClientDispatcher
        ?? throw new ArgumentNullException(nameof(realtimeClientDispatcher));

    public async Task<FlowChatResult<Unit>> Handle(PublishPresenceChangeCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty)
        {
            return FlowChatResult<Unit>.Failure(DomainError.BadRequest("UserId is required."));
        }

        if (string.IsNullOrWhiteSpace(request.Status))
        {
            return FlowChatResult<Unit>.Failure(DomainError.BadRequest("Status is required."));
        }

        var normalizedStatus = request.Status.Trim().ToLowerInvariant();
        if (!AllowedStatuses.Contains(normalizedStatus))
        {
            return FlowChatResult<Unit>.Failure(
                DomainError.BadRequest("Status must be one of: online, away, offline."));
        }

        var recipientUserIds = NormalizeRecipientUserIds(request.RecipientUserIds);
        if (recipientUserIds.Length == 0)
        {
            return FlowChatResult<Unit>.Failure(
                DomainError.BadRequest("RecipientUserIds must contain at least one valid user id."));
        }

        var notification = new PresenceChangedNotification(
            request.UserId,
            normalizedStatus,
            request.ChangedAtUtc,
            recipientUserIds);

        await _realtimeClientDispatcher.PresenceChangedAsync(notification, cancellationToken);

        return FlowChatResult<Unit>.Success(Unit.Value);
    }

    private static Guid[] NormalizeRecipientUserIds(IReadOnlyCollection<Guid> recipientUserIds) =>
        recipientUserIds
            .Where(userId => userId != Guid.Empty)
            .Distinct()
            .ToArray();
}

