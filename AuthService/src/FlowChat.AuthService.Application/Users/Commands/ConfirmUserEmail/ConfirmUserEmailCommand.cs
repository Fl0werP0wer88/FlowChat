using FlowChat.Application.Abstractions;
using MediatR;

namespace FlowChat.AuthService.Application.Users.Commands.ConfirmUserEmail;

public class ConfirmUserEmailCommand : ICommand<Unit>
{
    public required Guid UserId { get; set; }
    public required string Token { get; set; }
}
