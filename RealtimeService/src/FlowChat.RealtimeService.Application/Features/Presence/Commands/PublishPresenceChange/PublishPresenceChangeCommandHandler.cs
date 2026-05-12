using CSharpFunctionalExtensions;
using FlowChat.Core.Domain;
using FlowChat.Shared.Application;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using MediatR;

namespace FlowChat.RealtimeService.Application.Features.Presence.Commands.PublishPresenceChange;

public sealed class PublishPresenceChangeCommandHandler(IRealtimeClientDispatcher realtimeClientDispatcher)
    : ICommandHandler<PublishPresenceChangeCommand, Unit>
{
    private readonly IRealtimeClientDispatcher _realtimeClientDispatcher = realtimeClientDispatcher
        ?? throw new ArgumentNullException(nameof(realtimeClientDispatcher));

    public async Task<FlowChatResult<Unit>> Handle(PublishPresenceChangeCommand request, CancellationToken cancellationToken)
    {
        var recipientUserIds = NormalizeRecipientUserIds(request.RecipientUserIds);

        var notification = new PresenceChangedParam(
            request.UserId,
            request.Status,
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
