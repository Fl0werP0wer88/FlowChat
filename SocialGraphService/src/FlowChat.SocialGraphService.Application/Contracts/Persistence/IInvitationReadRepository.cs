using FlowChat.SocialGraphService.Domain.Entities;

namespace FlowChat.SocialGraphService.Application.Contracts.Persistence;

public interface IInvitationReadRepository
{
    Task<Invitation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Invitation>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> PendingBetweenUsersExistsAsync(
        Guid userAId,
        Guid userBId,
        CancellationToken cancellationToken = default);
}
