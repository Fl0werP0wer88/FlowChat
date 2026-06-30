using FlowChat.Core.Domain;
using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.PresenceService.Application.Features.Presence.Commands.ChangeUserPresencePreferences;

public sealed record ChangeUserPresencePreferencesCommand(
    Guid UserId,
    PresenceStatus Status) : ICommand<Unit>;
