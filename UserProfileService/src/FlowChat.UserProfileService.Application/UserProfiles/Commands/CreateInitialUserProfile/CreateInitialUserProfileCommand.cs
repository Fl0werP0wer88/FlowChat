using MediatR;

namespace FlowChat.UserProfileService.Application.UserProfiles.Commands.CreateInitialUserProfile;

public sealed record CreateInitialUserProfileCommand(
    string UserName,
    string DisplayName,
    string? AvatarUrl,
    string? Bio,
    Guid UserId) : IRequest<Guid>;
