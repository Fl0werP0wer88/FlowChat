using FlowChat.SocialGraphService.Application.Features.UserProfile;

namespace FlowChat.SocialGraphService.Application.Contracts.Persistence;

public interface IUserProfileProjectionRepository
{
    Task<bool> UpsertAsync(UserProfileProjection projection, CancellationToken cancellationToken = default);
}
