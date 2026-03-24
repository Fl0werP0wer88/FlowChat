using FlowChat.Application.Abstractions;

namespace FlowChat.UserProfileService.Application.Features.UserProfiles.Commands.CreateInitialUserProfile;

public sealed record CreateInitialUserProfileCommand(
    string UserName,
    string DisplayName,
    string? AvatarUrl,
    string? Bio,
    string? Email,
    string? Phone,
    Guid UserId) : ICommand<Guid>;
