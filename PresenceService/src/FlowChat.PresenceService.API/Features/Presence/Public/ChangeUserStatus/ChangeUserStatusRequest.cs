using FlowChat.Core.Contracts;
using FlowChat.Core.Domain;

namespace FlowChat.PresenceService.API.Features.Presence.Public.ChangeUserStatus;

public sealed class ChangeUserStatusRequest : IServiceInput
{
    public UserStatus Status { get; init; }
}
