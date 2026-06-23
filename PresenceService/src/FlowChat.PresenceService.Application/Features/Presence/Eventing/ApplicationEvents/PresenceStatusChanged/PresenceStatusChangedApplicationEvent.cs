using FlowChat.Core.Domain;
using MediatR;

namespace FlowChat.PresenceService.Application.Features.Presence.Eventing.ApplicationEvents.PresenceStatusChanged;

public sealed record PresenceStatusChangedApplicationEvent(
    Guid UserId,
    PresenceStatus Status,
    DateTimeOffset ChangedAtUtc) : INotification;
