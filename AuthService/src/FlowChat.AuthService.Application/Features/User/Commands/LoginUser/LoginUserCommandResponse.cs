using FlowChat.AuthService.Application.Features.User.Models;

namespace FlowChat.AuthService.Application.Features.User.Commands.LoginUser;

public sealed class LoginUserCommandResponse
{
    public required OpenIddictTokenGrantResult Grant { get; init; }
}
