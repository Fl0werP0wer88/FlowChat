using FlowChat.SocialGraphService.Application.Features.UserProfile;

namespace FlowChat.SocialGraphService.Application.Contracts.Persistence;

public interface IUserProfileProjectionReadRepository
{
    Task<UserProfileProjectionDto?> GetByUserProfileIdAsync(Guid userProfileId, CancellationToken cancellationToken = default);
    Task<UserProfileProjectionDto?> GetByFriendlyUserIdAsync(string friendlyUserId, CancellationToken cancellationToken = default);
    Task<UserProfileProjectionDto?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}

