using FlowChat.Core.Domain;
using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.RealtimeService.Application.Features.Presence.Commands.RoutePresenceChange;

public sealed record RoutePresenceChangeCommand(
    Guid UserId,
    PresenceStatus Status,
    DateTimeOffset ChangedAtUtc,
    IReadOnlyCollection<Guid> RecipientUserIds) : ICommand<Unit>;
