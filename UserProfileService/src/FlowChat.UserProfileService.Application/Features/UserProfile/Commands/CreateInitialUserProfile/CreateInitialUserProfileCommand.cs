using FlowChat.Shared.Application;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Commands.CreateInitialUserProfile;

public sealed record CreateInitialUserProfileCommand(
    string FriendlyUserId,
    string DisplayName,
    string? AvatarUrl,
    string? Bio,
    string? Email,
    string? Phone,
    Guid UserId,
    string? FirstName = null,
    string? LastName = null,
    string? Organization = null) : ICommand<Guid>;

