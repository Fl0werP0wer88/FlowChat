using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Application.Features.UserProfile;
using FlowChat.SocialGraphService.Application.Features.UserProfile.Commands.BulkUpsertOrDeleteUserProfileProjection;
using FlowChat.SocialGraphService.Persistence.Entities;
using FlowChat.Shared.Persistance.BulkUpsert;

namespace FlowChat.SocialGraphService.Persistence.BulkUpsert;

public sealed class UserProfileProjectionBulkRepository(AppDbContext dbContext)
    : ProjectionBulkRepositoryBase<AppDbContext, UserProfileProjectionCommandItem, UserProfileProjectionDto, UserProfileReadModelEntity>(dbContext),
        IUserProfileProjectionBulkRepository
{
    private const string TombstoneSource = "user-profile-projection";

    public Task BulkUpsertOrSoftDeleteAsync(
        IReadOnlyCollection<UserProfileProjectionCommandItem> items,
        CancellationToken cancellationToken) =>
        BulkUpsertProjectionAsync(
            items,
            [nameof(UserProfileReadModelEntity.UserProfileId)],
            cancellationToken);

    protected override UserProfileProjectionDto? GetValue(UserProfileProjectionCommandItem item) =>
        item.Value;

    protected override int GetSourceVersion(UserProfileProjectionCommandItem item) =>
        item.SourceVersion;

    protected override UserProfileReadModelEntity CreateUpsertEntity(
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
            DeletedAt = null,
            CreatedBy = item.Source,
            CreatedAtUtc = now,
            LastModifiedBy = item.Source,
            LastModifiedAtUtc = now
        };

    protected override UserProfileReadModelEntity CreateTombstoneEntity(
        UserProfileProjectionCommandItem item,
        DateTimeOffset now) =>
        new()
        {
            UserProfileId = item.EntityId.Value,
            FriendlyUserId = string.Empty,
            IsActive = false,
            SourceVersion = item.SourceVersion,
            DeletedAt = now,
            CreatedBy = TombstoneSource,
            CreatedAtUtc = now,
            LastModifiedBy = TombstoneSource,
            LastModifiedAtUtc = now
        };
}
