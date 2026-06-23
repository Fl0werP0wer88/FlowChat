using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.AuthService.Application.Features.User.Commands.ChangeAuthEmail;

public sealed class ChangeAuthEmailCommand : ICommand<Unit>
{
    public Guid UserId { get; init; }
    public required string EmailAddress { get; init; }
}
