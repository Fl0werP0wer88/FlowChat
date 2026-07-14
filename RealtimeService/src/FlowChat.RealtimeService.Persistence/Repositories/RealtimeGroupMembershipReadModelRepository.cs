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

    public async Task AddRangeAsync(
        IReadOnlyCollection<Guid> userIds,
        RealtimeGroupType groupType,
        Guid resourceId,
        CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0)
        {
            return;
        }

        var existingUserIds = await dbContext.RealtimeGroupMembershipReadModels
            .Where(x => x.GroupType == groupType && x.ResourceId == resourceId && userIds.Contains(x.UserId))
            .Select(x => x.UserId)
            .ToListAsync(cancellationToken);

        var newUserIds = userIds.Except(existingUserIds);
        var createdAt = DateTimeOffset.UtcNow;

        dbContext.RealtimeGroupMembershipReadModels.AddRange(
            newUserIds.Select(userId => RealtimeGroupMembershipReadModel.Create(userId, groupType, resourceId, createdAt)));

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveRangeAsync(
        IReadOnlyCollection<Guid> userIds,
        RealtimeGroupType groupType,
        Guid resourceId,
        CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0)
        {
            return;
        }

        var memberships = await dbContext.RealtimeGroupMembershipReadModels
            .Where(x => x.GroupType == groupType && x.ResourceId == resourceId && userIds.Contains(x.UserId))
            .ToListAsync(cancellationToken);
        if (memberships.Count == 0)
        {
            return;
        }

        dbContext.RealtimeGroupMembershipReadModels.RemoveRange(memberships);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
