using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.PresenceService.Application.Features.Presence.Commands.SoftDeleteUserPresencePreferences;

public sealed record SoftDeleteUserPresencePreferencesCommand(Guid UserId) : ICommand<Unit>;
