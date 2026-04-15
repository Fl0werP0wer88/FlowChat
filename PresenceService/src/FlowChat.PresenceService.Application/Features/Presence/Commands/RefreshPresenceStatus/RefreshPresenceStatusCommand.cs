using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.PresenceService.Application.Features.Presence.Commands.RefreshPresenceStatus;

public sealed record RefreshPresenceStatusCommand(IReadOnlyCollection<Guid> UserIds) : ICommand<Unit>;
