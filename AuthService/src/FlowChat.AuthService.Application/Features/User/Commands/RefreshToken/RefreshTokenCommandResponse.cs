using FlowChat.AuthService.Application.Features.User.Models;

namespace FlowChat.AuthService.Application.Features.User.Commands.RefreshToken;

public sealed class RefreshTokenCommandResponse
{
    public required OpenIddictTokenGrantResult Grant { get; init; }
}
