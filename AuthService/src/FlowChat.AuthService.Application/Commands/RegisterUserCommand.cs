using FlowChat.AuthService.Application.Responses;
using MediatR;

namespace FlowChat.AuthService.Application.Commands;

public class RegisterUserCommand : IRequest<RegisterUserCommandResponse>
{
    public required string UserName { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
}
