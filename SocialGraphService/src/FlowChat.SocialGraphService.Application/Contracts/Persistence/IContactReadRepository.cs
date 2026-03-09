using FlowChat.SocialGraphService.Domain.Entities;

namespace FlowChat.SocialGraphService.Application.Contracts.Persistence;

public interface IContactReadRepository
{
    Task<Contact?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Contact>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Contact?> GetWithUsersAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Contact>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> RelationshipExistsAsync(
        Guid ownerUserId,
        Guid contactUserId,
        CancellationToken cancellationToken = default);
}
