using FlowChat.SocialGraphService.Domain.Entities;

namespace FlowChat.SocialGraphService.Application.Contracts.Persistence;

public interface IUserSocialGraphRepository
{
    Task<UserSocialGraph?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserSocialGraph> AddAsync(UserSocialGraph entity, CancellationToken cancellationToken = default);
    Task<Contact> AddContactAsync(Contact entity, CancellationToken cancellationToken = default);
    Task UpdateContactAsync(Contact entity, CancellationToken cancellationToken = default);
    Task DeleteContactAsync(Contact entity, CancellationToken cancellationToken = default);
    Task<Invitation> AddInvitationAsync(Guid userSocialGraphId, Invitation entity, CancellationToken cancellationToken = default);
    Task UpdateInvitationAsync(Invitation entity, CancellationToken cancellationToken = default);
    Task DeleteInvitationAsync(Invitation entity, CancellationToken cancellationToken = default);
}
