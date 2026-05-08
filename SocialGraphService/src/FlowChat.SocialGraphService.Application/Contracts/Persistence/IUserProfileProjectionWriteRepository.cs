using FlowChat.SocialGraphService.Application.Features.UserProfile;

namespace FlowChat.SocialGraphService.Application.Contracts.Persistence;

public interface IUserProfileProjectionWriteRepository
{
    Task InsertAsync(UserProfileProjectionDto projection, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(UserProfileProjectionDto projection, CancellationToken cancellationToken = default);
}

