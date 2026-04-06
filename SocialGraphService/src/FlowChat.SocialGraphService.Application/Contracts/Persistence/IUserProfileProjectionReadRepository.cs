using FlowChat.SocialGraphService.Application.Features.UserProfile;

namespace FlowChat.SocialGraphService.Application.Contracts.Persistence;

public interface IUserProfileProjectionReadRepository
{
    Task<IReadOnlyList<UserProfileProjection>> SearchAsync(
        string? firstName,
        string? lastName,
        string? organization,
        CancellationToken cancellationToken = default);
}
