using CSharpFunctionalExtensions;
using FlowChat.Core.Domain;
using FlowChat.Shared.Application;
using FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;
using FlowChat.Shared.Domain;
using MediatR;

namespace FlowChat.RealtimeService.Application.Features.Presence.Commands.PublishPresenceChange;

public sealed class PublishPresenceChangeCommandHandler(IRealtimeClientDispatcher realtimeClientDispatcher)
    : ICommandHandler<PublishPresenceChangeCommand, Unit>
{
    private readonly IRealtimeClientDispatcher _realtimeClientDispatcher = realtimeClientDispatcher
        ?? throw new ArgumentNullException(nameof(realtimeClientDispatcher));

    public async Task<FlowChatResult<Unit>> Handle(PublishPresenceChangeCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty)
        {
            return FlowChatResult<Unit>.Failure(DomainError.BadRequest("UserId is required."));
        }

        if (!Enum.IsDefined(typeof(PresenceStatus), request.Status))
        {
            return FlowChatResult<Unit>.Failure(
                DomainError.BadRequest("Status must be one of: Active, AFK, Busy, Invisible."));
        }

        var recipientUserIds = NormalizeRecipientUserIds(request.RecipientUserIds);
        if (recipientUserIds.Length == 0)
        {
            return FlowChatResult<Unit>.Failure(
                DomainError.BadRequest("RecipientUserIds must contain at least one valid user id."));
        }

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
