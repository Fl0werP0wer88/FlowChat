using FlowChat.Application.Abstractions;

namespace FlowChat.AuthService.Application.Users.Commands.ConfirmUserEmail;

public class ConfirmUserEmailCommand : ICommand<ConfirmUserEmailCommandResponse>
{
    public required Guid UserId { get; set; }
    public required string Token { get; set; }
}
