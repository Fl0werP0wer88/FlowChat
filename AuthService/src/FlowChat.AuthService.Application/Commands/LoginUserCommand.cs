using FlowChat.AuthService.Application.Responses;
using MediatR;

namespace FlowChat.AuthService.Application.Commands;

public class LoginUserCommand : IRequest<LoginUserCommandResponse>
{
    public required string Login { get; set; }
    public required string Password { get; set; }
}
