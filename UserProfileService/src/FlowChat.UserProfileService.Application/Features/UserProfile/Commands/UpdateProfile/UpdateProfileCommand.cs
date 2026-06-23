using FlowChat.Shared.Application;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Commands.UpdateProfile;

public sealed record UpdateProfileCommand(
    Guid UserId,
    string? FirstName,
    string? LastName,
    string? Organization,
    string? AvatarUrl,
    string? Bio,
    bool IsActive) : ICommand<Guid>;
