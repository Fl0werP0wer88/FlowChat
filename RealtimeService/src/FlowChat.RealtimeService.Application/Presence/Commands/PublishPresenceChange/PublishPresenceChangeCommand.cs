using MediatR;

namespace FlowChat.RealtimeService.Application.Presence.Commands.PublishPresenceChange;

public sealed record PublishPresenceChangeCommand(
    Guid UserId,
    string? Status,
    DateTime ChangedAtUtc,
    IReadOnlyCollection<Guid> RecipientUserIds) : IRequest;
