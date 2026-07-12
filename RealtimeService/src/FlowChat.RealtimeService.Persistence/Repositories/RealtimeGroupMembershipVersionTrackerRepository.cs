using FlowChat.RealtimeService.Application.Contracts.Persistence;
using FlowChat.RealtimeService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.RealtimeService.Persistence.Repositories;

public sealed class RealtimeGroupMembershipVersionTrackerRepository(AppDbContext dbContext)
    : IRealtimeGroupMembershipVersionTrackerRepository
{
    public async Task<int?> GetVersionAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.RealtimeGroupMembershipVersionTrackerReadModels
            .Where(x => x.ConversationId == conversationId)
            .Select(x => (int?)x.Version)
            .FirstOrDefaultAsync(cancellationToken);
    }

    //ToDo1: pomyslec czy nie da rady tu zrobic faktycznego upsertu na poziomie bazy a nie EF.
    public async Task UpsertIfNewerAsync(
        Guid conversationId,
        int version,
        CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.RealtimeGroupMembershipVersionTrackerReadModels
            .FirstOrDefaultAsync(x => x.ConversationId == conversationId, cancellationToken);

        var updatedAt = DateTimeOffset.UtcNow;

        if (existing is null)
        {
            dbContext.RealtimeGroupMembershipVersionTrackerReadModels.Add(
                RealtimeGroupMembershipVersionTrackerReadModel.Create(conversationId, version, updatedAt));

            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        if (version <= existing.Version)
        {
            return;
        }

        existing.UpdateVersion(version, updatedAt);
        //ToDo1: To sie bedzie chyba wykonywalo w transakcji command handlera. Upewnic sie ze tak jest iwywalic save async.
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
