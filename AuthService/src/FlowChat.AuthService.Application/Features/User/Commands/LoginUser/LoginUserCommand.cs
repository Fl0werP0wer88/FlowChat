using FlowChat.Shared.Application;

namespace FlowChat.AuthService.Application.Features.User.Commands.LoginUser;

public class LoginUserCommand : ICommand<LoginUserCommandResponse>
{
    public required string Login { get; set; }
    public required string Password { get; set; }
}

