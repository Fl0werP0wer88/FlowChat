using FlowChat.RealtimeService.Application.Contracts.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.RealtimeService.Persistence.Repositories;

public sealed class RealtimeGroupMembershipRevisionTrackerRepository(AppDbContext dbContext)
    : IRealtimeGroupMembershipRevisionTrackerRepository
{
    public async Task<int?> GetRevisionAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.RealtimeGroupMembershipRevisionTrackerReadModels
            .Where(x => x.ConversationId == conversationId)
            .Select(x => (int?)x.Revision)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task UpsertIfNewerAsync(
        Guid conversationId,
        int revision,
        CancellationToken cancellationToken = default)
    {
        var updatedAt = DateTimeOffset.UtcNow;

        // PostgreSQL handles the conditional upsert atomically, avoiding races between a separate EF read and write (Older version could overwite newer without this)
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO "RealtimeGroupMembershipRevisionTrackerReadModels"
                 ("ConversationId", "Revision", "UpdatedAt")
             VALUES
                 ({conversationId}, {revision}, {updatedAt})
             ON CONFLICT ("ConversationId")
             DO UPDATE SET
                 "Revision" = EXCLUDED."Revision",
                 "UpdatedAt" = EXCLUDED."UpdatedAt"
             WHERE EXCLUDED."Revision" > "RealtimeGroupMembershipRevisionTrackerReadModels"."Revision";
             """,
            cancellationToken);
    }
}
