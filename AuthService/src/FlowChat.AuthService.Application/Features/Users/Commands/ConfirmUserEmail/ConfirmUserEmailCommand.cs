using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.AuthService.Application.Features.Users.Commands.ConfirmUserEmail;

public class ConfirmUserEmailCommand : ICommand<Unit>
{
    public required Guid UserId { get; set; }
    public required string Token { get; set; }
}

