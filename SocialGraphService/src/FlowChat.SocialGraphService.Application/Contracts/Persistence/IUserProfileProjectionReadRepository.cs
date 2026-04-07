using FlowChat.SocialGraphService.Application.Features.UserProfile;

namespace FlowChat.SocialGraphService.Application.Contracts.Persistence;

public interface IUserProfileProjectionReadRepository
{
    Task<UserProfileProjection?> GetByUserProfileIdAsync(Guid userProfileId, CancellationToken cancellationToken = default);
    Task<UserProfileProjection?> GetByFriendlyUserIdAsync(string friendlyUserId, CancellationToken cancellationToken = default);
    Task<UserProfileProjection?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserProfileProjection>> SearchAsync(
        string? firstName,
        string? lastName,
        string? organization,
        CancellationToken cancellationToken = default);
}
