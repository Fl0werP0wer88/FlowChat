using FlowChat.Core.Results;
using FlowChat.Shared.Application;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Commands.CreateInitialUserProfile;

public sealed record CreateInitialUserProfileCommand(
    string FriendlyUserId,
    string? Email,
    Guid UserId,
    string? FirstName = null,
    string? LastName = null,
    string? Organization = null) : ICommand<IdempotentCommandResult<Guid>>
{
    public const string IdempotencyConflictKey = nameof(CreateInitialUserProfileCommand);
}

