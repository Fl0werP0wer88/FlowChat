using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.AuthService.Application.Features.Users.Commands.ConfirmAuthEmail;

public sealed class ConfirmAuthEmailCommand : ICommand<Unit>
{
    public required string EmailAddress { get; init; }
}
