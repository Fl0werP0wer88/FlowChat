using FlowChat.RealtimeService.Application.Contracts.Persistence;
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

    public async Task UpsertIfNewerAsync(
        Guid conversationId,
        int version,
        CancellationToken cancellationToken = default)
    {
        var updatedAt = DateTimeOffset.UtcNow;

        // PostgreSQL handles the conditional upsert atomically, avoiding races between a separate EF read and write
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO "RealtimeGroupMembershipVersionTrackerReadModels"
                 ("ConversationId", "Version", "UpdatedAt")
             VALUES
                 ({conversationId}, {version}, {updatedAt})
             ON CONFLICT ("ConversationId")
             DO UPDATE SET
                 "Version" = EXCLUDED."Version",
                 "UpdatedAt" = EXCLUDED."UpdatedAt"
             WHERE EXCLUDED."Version" > "RealtimeGroupMembershipVersionTrackerReadModels"."Version";
             """,
            cancellationToken);
    }
}
