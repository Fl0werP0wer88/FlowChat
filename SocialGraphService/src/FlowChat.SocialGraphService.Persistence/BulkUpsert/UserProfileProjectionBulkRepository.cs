using EFCore.BulkExtensions;
using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Application.Features.UserProfile;
using FlowChat.SocialGraphService.Application.Features.UserProfile.Commands.BulkUpsertOrDeleteUserProfileProjection;
using FlowChat.SocialGraphService.Persistence.Entities;

namespace FlowChat.SocialGraphService.Persistence.BulkUpsert;

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
                UpdateByProperties = [nameof(UserProfileReadModelEntity.UserProfileId)],
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
            UserProfileId = item.UserProfileId,
            FriendlyUserId = item.FriendlyUserId,
            FirstName = item.FirstName,
            LastName = item.LastName,
            Organization = item.Organization,
            MainEmail = item.MainEmail?.Address,
            MainEmailIsConfirmed = item.MainEmail?.IsConfirmed,
            MainEmailIsVisible = item.MainEmail?.IsVisible,
            MainPhone = item.MainPhone?.Number,
            MainPhoneIsConfirmed = item.MainPhone?.IsConfirmed,
            MainPhoneIsVisible = item.MainPhone?.IsVisible,
            AvatarUrl = item.AvatarUrl,
            Bio = item.Bio,
            IsActive = item.IsActive,
            LastSeenAtUtc = item.LastSeenAtUtc,
            SourceVersion = sourceVersion,
            IsDeleted = false,
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
            UserProfileId = item.EntityId.Value,
            FriendlyUserId = string.Empty,
            IsActive = false,
            SourceVersion = item.SourceVersion,
            IsDeleted = true,
            CreatedBy = TombstoneSource,
            CreatedAtUtc = now,
            LastModifiedBy = TombstoneSource,
            LastModifiedAtUtc = now
        };
}
