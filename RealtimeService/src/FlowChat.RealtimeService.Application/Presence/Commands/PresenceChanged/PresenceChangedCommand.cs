using MediatR;

namespace FlowChat.RealtimeService.Application.Presence.Commands.PresenceChanged;

public sealed record PresenceChangedCommand(
    Guid UserId,
    string? Status,
    DateTime ChangedAtUtc,
    IReadOnlyCollection<Guid> RecipientUserIds) : IRequest;
