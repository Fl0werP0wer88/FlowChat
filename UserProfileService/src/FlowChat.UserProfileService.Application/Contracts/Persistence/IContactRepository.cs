using FlowChat.UserProfileService.Domain.Entities;
using FlowChat.UserProfileService.Domain.Enums;

namespace FlowChat.UserProfileService.Application.Contracts.Persistence;

public interface IContactRepository : IAsyncRepository<Contact>
{
    Task<Contact?> GetWithUsersAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Contact>> GetForUserAsync(
        Guid userId,
        ContactStatus? status = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Contact>> GetIncomingPendingAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
    Task<bool> RelationshipExistsAsync(
        Guid userAId,
        Guid userBId,
        CancellationToken cancellationToken = default);
}
