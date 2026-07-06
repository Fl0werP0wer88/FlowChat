using FlowChat.RealtimeService.Application.Contracts.Persistence;
using FlowChat.RealtimeService.Domain.Enums;
using FlowChat.RealtimeService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.RealtimeService.Persistence.Repositories;

public sealed class RealtimeGroupMembershipRepository(AppDbContext dbContext) : IRealtimeGroupMembershipRepository
{
    public async Task<IReadOnlyList<RealtimeGroupMembershipDto>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.RealtimeGroupMemberships
            .Where(x => x.UserId == userId)
            .Select(x => new RealtimeGroupMembershipDto(x.UserId, x.GroupType, x.ResourceId, x.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        Guid userId,
        RealtimeGroupType groupType,
        Guid resourceId,
        CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.RealtimeGroupMemberships.AnyAsync(
            x => x.UserId == userId && x.GroupType == groupType && x.ResourceId == resourceId,
            cancellationToken);
        if (exists)
        {
            return;
        }

        dbContext.RealtimeGroupMemberships.Add(
            RealtimeGroupMembership.Create(userId, groupType, resourceId, DateTimeOffset.UtcNow));

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(
        Guid userId,
        RealtimeGroupType groupType,
        Guid resourceId,
        CancellationToken cancellationToken = default)
    {
        var membership = await dbContext.RealtimeGroupMemberships.FirstOrDefaultAsync(
            x => x.UserId == userId && x.GroupType == groupType && x.ResourceId == resourceId,
            cancellationToken);
        if (membership is null)
        {
            return;
        }

        dbContext.RealtimeGroupMemberships.Remove(membership);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
