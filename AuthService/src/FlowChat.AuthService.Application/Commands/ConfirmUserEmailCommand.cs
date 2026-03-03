using FlowChat.Application.Abstractions;
using FlowChat.AuthService.Application.Responses;

namespace FlowChat.AuthService.Application.Commands;

public class ConfirmUserEmailCommand : ICommand<ConfirmUserEmailCommandResponse>
{
    public required Guid UserId { get; set; }
    public required string Token { get; set; }
}
