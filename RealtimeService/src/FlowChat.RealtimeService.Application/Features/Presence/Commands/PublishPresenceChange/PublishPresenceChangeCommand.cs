using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.RealtimeService.Application.Features.Presence.Commands.PublishPresenceChange;

public sealed record PublishPresenceChangeCommand(
    Guid UserId,
    string? Status,
    DateTime ChangedAtUtc,
    IReadOnlyCollection<Guid> RecipientUserIds) : ICommand<Unit>;

