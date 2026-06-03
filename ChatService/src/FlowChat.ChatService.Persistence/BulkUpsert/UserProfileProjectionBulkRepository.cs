using EFCore.BulkExtensions;
using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.UserProfile;
using FlowChat.ChatService.Application.Features.UserProfile.Commands.BulkUpsertOrDeleteUserProfileProjection;
using FlowChat.ChatService.Persistence.Entities;

namespace FlowChat.ChatService.Persistence.BulkUpsert;

public sealed class UserProfileProjectionBulkRepository(AppDbContext dbContext)
    : IUserProfileProjectionBulkRepository
{
    private const string TombstoneSource = "user-profile-projection";

    public async Task BulkUpsertOrSoftDeleteAsync(
        IReadOnlyCollection<UserProfileProjectionCommandItem> items,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var entities = items.Select(item => CreateEntity(item, now)).ToList();

        await dbContext.BulkInsertOrUpdateAsync(
            entities,
            new BulkConfig
            {
                // flowchat_app has CRUD-only access; regular helper tables require CREATE on the public schema
                UseTempDB = true,
                UpdateByProperties = [nameof(UserProfileReadModelEntity.UserId)],
                OnConflictUpdateWhereSql = (existing, inserted) =>
                    $"{inserted}.\"SourceVersion\" > {existing}.\"SourceVersion\"",
                PropertiesToExcludeOnUpdate =
                [
                    nameof(UserProfileReadModelEntity.CreatedBy),
                    nameof(UserProfileReadModelEntity.CreatedAtUtc)
                ]
            },
            cancellationToken: cancellationToken);
    }

    private static UserProfileReadModelEntity CreateEntity(
        UserProfileProjectionCommandItem item,
        DateTimeOffset now) =>
        item.Value is null
            ? CreateTombstoneEntity(item, now)
            : CreateUpsertEntity(item.Value, item.SourceVersion, now);

    private static UserProfileReadModelEntity CreateUpsertEntity(
        UserProfileProjectionDto item,
        int sourceVersion,
        DateTimeOffset now) =>
        new()
        {
            UserId = item.UserProfileId,
            FriendlyUserId = item.FriendlyUserId,
            FirstName = item.FirstName,
            LastName = item.LastName,
            AvatarUrl = item.AvatarUrl,
            SourceVersion = sourceVersion,
            DeletedAt = null,
            CreatedBy = item.Source,
            CreatedAtUtc = now,
            LastModifiedBy = item.Source,
            LastModifiedAtUtc = now
        };

    private static UserProfileReadModelEntity CreateTombstoneEntity(
        UserProfileProjectionCommandItem item,
        DateTimeOffset now) =>
        new()
        {
            UserId = item.EntityId.Value,
            FriendlyUserId = string.Empty,
            SourceVersion = item.SourceVersion,
            DeletedAt = now,
            CreatedBy = TombstoneSource,
            CreatedAtUtc = now,
            LastModifiedBy = TombstoneSource,
            LastModifiedAtUtc = now
        };
}
