using FlowChat.Core.Domain;
using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.PresenceService.Application.Features.Presence.Commands.ChangeUserStatus;

public sealed record ChangeUserStatusCommand(
    Guid UserId,
    UserStatus Status) : ICommand<Unit>;
