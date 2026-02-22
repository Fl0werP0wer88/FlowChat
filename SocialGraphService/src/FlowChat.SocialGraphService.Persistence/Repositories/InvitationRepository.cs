using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Domain.Entities;
using FlowChat.SocialGraphService.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.SocialGraphService.Persistence.Repositories;

public sealed class InvitationRepository : RepositoryBase<Invitation>, IInvitationRepository
{
    public InvitationRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<bool> PendingBetweenUsersExistsAsync(
        Guid userAId,
        Guid userBId,
        CancellationToken cancellationToken = default)
    {
        return await DbContext.Invitations.AnyAsync(
            x => x.Status == InvitationStatus.Pending
                 && ((x.RequesterId == userAId && x.AddresseeId == userBId)
                     || (x.RequesterId == userBId && x.AddresseeId == userAId)),
            cancellationToken);
    }
}
