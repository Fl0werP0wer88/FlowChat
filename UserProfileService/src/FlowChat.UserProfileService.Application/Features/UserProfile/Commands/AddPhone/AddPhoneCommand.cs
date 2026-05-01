using FlowChat.Shared.Application;
using FlowChat.Core.Results;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Commands.AddPhone;

public sealed record AddPhoneCommand(Guid UserId, Guid PhoneId, string? Number)
    : ICommand<IdempotentCommandResult<Guid>>
{
    public const string IdempotencyConflictKey = nameof(AddPhoneCommand);
}

