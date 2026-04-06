using FlowChat.SocialGraphService.Application.Features.UserProfile;

namespace FlowChat.SocialGraphService.Application.Contracts.Persistence;

public interface IUserProfileProjectionRepository
{
    Task<bool> InsertAsync(UserProfileProjection projection, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(UserProfileProjection projection, CancellationToken cancellationToken = default);
}
