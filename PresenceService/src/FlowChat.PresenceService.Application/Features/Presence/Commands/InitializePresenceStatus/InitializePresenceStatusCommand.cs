using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.PresenceService.Application.Features.Presence.Commands.InitializePresenceStatus;

public sealed record InitializePresenceStatusCommand(Guid UserId) : ICommand<Unit>;
