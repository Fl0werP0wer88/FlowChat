using FlowChat.RealtimeService.Application.Contracts.Persistence;
using FlowChat.RealtimeService.Domain.Enums;
using FlowChat.RealtimeService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.RealtimeService.Persistence.Repositories;

public sealed class RealtimeGroupMembershipReadModelRepository(AppDbContext dbContext) : IRealtimeGroupMembershipReadModelRepository
{
    public async Task<IReadOnlyList<RealtimeGroupMembershipReadModelDto>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.RealtimeGroupMembershipReadModels
            .Where(x => x.UserId == userId)
            .Select(x => new RealtimeGroupMembershipReadModelDto(x.UserId, x.GroupType, x.ResourceId, x.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetUserIdsByResourceIdAsync(
        RealtimeGroupType groupType,
        Guid resourceId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.RealtimeGroupMembershipReadModels
            .Where(x => x.GroupType == groupType && x.ResourceId == resourceId)
            .Select(x => x.UserId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        Guid userId,
        RealtimeGroupType groupType,
        Guid resourceId,
        CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.RealtimeGroupMembershipReadModels.AnyAsync(
            x => x.UserId == userId && x.GroupType == groupType && x.ResourceId == resourceId,
            cancellationToken);
        if (exists)
        {
            return;
        }

        dbContext.RealtimeGroupMembershipReadModels.Add(
            RealtimeGroupMembershipReadModel.Create(userId, groupType, resourceId, DateTimeOffset.UtcNow));

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(
        Guid userId,
        RealtimeGroupType groupType,
        Guid resourceId,
        CancellationToken cancellationToken = default)
    {
        var membership = await dbContext.RealtimeGroupMembershipReadModels.FirstOrDefaultAsync(
            x => x.UserId == userId && x.GroupType == groupType && x.ResourceId == resourceId,
            cancellationToken);
        if (membership is null)
        {
            return;
        }

        dbContext.RealtimeGroupMembershipReadModels.Remove(membership);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
