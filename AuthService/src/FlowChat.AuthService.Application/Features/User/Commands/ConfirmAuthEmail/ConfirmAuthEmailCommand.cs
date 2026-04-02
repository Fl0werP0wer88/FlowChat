using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.AuthService.Application.Features.User.Commands.ConfirmAuthEmail;

public sealed class ConfirmAuthEmailCommand : ICommand<Unit>
{
    public required string EmailAddress { get; init; }
}
