using FlowChat.SocialGraphService.Domain.Entities;

namespace FlowChat.SocialGraphService.Application.Contracts.Persistence;

public interface IUserSocialGraphRepository
{
    Task<UserSocialGraph?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserSocialGraph> AddAsync(UserSocialGraph entity, CancellationToken cancellationToken = default);
}
