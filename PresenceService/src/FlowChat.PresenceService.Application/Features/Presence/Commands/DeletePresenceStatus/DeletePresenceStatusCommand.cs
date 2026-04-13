using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.PresenceService.Application.Features.Presence.Commands.DeletePresenceStatus;

public sealed record DeletePresenceStatusCommand(Guid UserId) : ICommand<Unit>;
