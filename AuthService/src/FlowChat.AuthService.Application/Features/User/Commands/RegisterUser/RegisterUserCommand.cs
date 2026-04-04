using FlowChat.Shared.Application;
namespace FlowChat.AuthService.Application.Features.User.Commands.RegisterUser;

public class RegisterUserCommand : ICommand<RegisterUserCommandResponse>
{
    public required string FriendlyUserId { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
}

