using FlowChat.SocialGraphService.Application.Features.UserProfile;

namespace FlowChat.SocialGraphService.Application.Contracts.Persistence;

public interface IUserProfileReadModelRepository
{
    Task<bool> UpsertAsync(UserProfileReadModel readModel, CancellationToken cancellationToken = default);
}
