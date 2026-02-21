using FlowChat.SocialGraphService.Domain.Entities;
using FlowChat.SocialGraphService.Domain.Enums;

namespace FlowChat.SocialGraphService.Application.Contracts.Persistence;

public interface IContactRepository : IAsyncRepository<Contact>
{
    Task<Contact?> GetWithUsersAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Contact>> GetForUserAsync(
        Guid userId,
        InvitationStatus? status = null,
        CancellationToken cancellationToken = default);
    Task<bool> RelationshipExistsAsync(
        Guid userAId,
        Guid userBId,
        CancellationToken cancellationToken = default);
}
