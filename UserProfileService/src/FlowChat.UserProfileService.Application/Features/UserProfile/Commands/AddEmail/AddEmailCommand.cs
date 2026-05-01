using FlowChat.Shared.Application;
using FlowChat.Core.Results;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Commands.AddEmail;

public sealed record AddEmailCommand(Guid UserId, Guid EmailId, string? Address)
    : ICommand<IdempotentCommandResult<Guid>>
{
    public const string IdempotencyConflictKey = nameof(AddEmailCommand);
}
