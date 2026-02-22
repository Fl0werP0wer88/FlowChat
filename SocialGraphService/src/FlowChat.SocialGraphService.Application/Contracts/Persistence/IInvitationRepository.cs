using FlowChat.SocialGraphService.Domain.Entities;

namespace FlowChat.SocialGraphService.Application.Contracts.Persistence;

public interface IInvitationRepository : IAsyncRepository<Invitation>
{
    Task<bool> PendingBetweenUsersExistsAsync(
        Guid userAId,
        Guid userBId,
        CancellationToken cancellationToken = default);
}
