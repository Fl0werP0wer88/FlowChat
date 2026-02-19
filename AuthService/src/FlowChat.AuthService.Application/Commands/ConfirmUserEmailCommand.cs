using FlowChat.AuthService.Application.Responses;
using MediatR;

namespace FlowChat.AuthService.Application.Commands;

public class ConfirmUserEmailCommand : IRequest<ConfirmUserEmailCommandResponse>
{
    public required Guid UserId { get; set; }
    public required string Token { get; set; }
}
