using FlowChat.Shared.Application;
namespace FlowChat.AuthService.Application.Features.Users.Commands.RegisterUser;

public class RegisterUserCommand : ICommand<RegisterUserCommandResponse>
{
    public required string UserName { get; set; }
    public required string Email { get; set; }
    public required string? PhoneNumber { get; set; }
    public required string Password { get; set; }
}

