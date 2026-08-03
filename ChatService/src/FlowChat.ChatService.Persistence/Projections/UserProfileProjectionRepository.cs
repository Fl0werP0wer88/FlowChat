using FlowChat.ChatService.Application.Features.UserProfile;
using FlowChat.Core.Messaging;
using FlowChat.Shared.Application;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.ChatService.Persistence.Projections;

public sealed class UserProfileProjectionRepository(
    AppDbContext dbContext,
    TimeProvider timeProvider) : IProjectionRepository<UserProfileProjectionDto>
{
    public Task UpsertOrSoftDeleteAsync(
        ProjectionCommandItem<UserProfileProjectionDto> item,
        CancellationToken cancellationToken)
    {
        var value = item.Value;
        var sourceDeletedAtUtc = item.Operation == OperationType.Deleted
            ? item.SourceDeletedAtUtc ?? timeProvider.GetUtcNow()
            : item.SourceDeletedAtUtc;

        return dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO "UserProfileReadModel"
                ("UserId", "FriendlyUserId", "FirstName", "LastName", "AvatarUrl", "Email",
                 "SourceVersion", "SourceCreatedAtUtc", "SourceLastModifiedAtUtc", "SourceDeletedAtUtc")
            VALUES
                ({value.UserProfileId}, {value.FriendlyUserId}, {value.FirstName}, {value.LastName},
                 {value.AvatarUrl}, {value.Email}, {item.SourceVersion}, {item.SourceCreatedAtUtc},
                 {item.SourceLastModifiedAtUtc}, {sourceDeletedAtUtc})
            ON CONFLICT ("UserId") DO UPDATE SET
                "FriendlyUserId" = EXCLUDED."FriendlyUserId",
                "FirstName" = EXCLUDED."FirstName",
                "LastName" = EXCLUDED."LastName",
                "AvatarUrl" = EXCLUDED."AvatarUrl",
                "Email" = EXCLUDED."Email",
                "SourceVersion" = EXCLUDED."SourceVersion",
                "SourceCreatedAtUtc" = EXCLUDED."SourceCreatedAtUtc",
                "SourceLastModifiedAtUtc" = EXCLUDED."SourceLastModifiedAtUtc",
                "SourceDeletedAtUtc" = EXCLUDED."SourceDeletedAtUtc"
            WHERE EXCLUDED."SourceVersion" > "UserProfileReadModel"."SourceVersion";
            """,
            cancellationToken);
    }
}
