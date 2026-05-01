using FlowChat.Shared.Application;
using FlowChat.Core.Results;
namespace FlowChat.AuthService.Application.Features.User.Commands.RegisterUser;

public class RegisterUserCommand : ICommand<IdempotentCommandResult<RegisterUserCommandResponse>>
{
    public const string IdempotencyConflictKey = nameof(RegisterUserCommand);

    public required Guid Id { get; set; }
    public required string FriendlyUserId { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Organization { get; set; }
}

