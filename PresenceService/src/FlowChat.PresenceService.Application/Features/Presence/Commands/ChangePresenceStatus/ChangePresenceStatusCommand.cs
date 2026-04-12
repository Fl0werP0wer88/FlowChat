using FlowChat.Core.Domain;
using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.PresenceService.Application.Features.Presence.Commands.ChangePresenceStatus;

public sealed record ChangePresenceStatusCommand(
    Guid UserId,
    PresenceStatus Status) : ICommand<Unit>;
