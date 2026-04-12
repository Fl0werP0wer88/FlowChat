using FlowChat.Core.Domain;
using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.RealtimeService.Application.Features.Presence.Commands.PublishPresenceChange;

public sealed record PublishPresenceChangeCommand(
    Guid UserId,
    UserPresenceStatus Status,
    DateTimeOffset ChangedAtUtc,
    IReadOnlyCollection<Guid> RecipientUserIds) : ICommand<Unit>;

