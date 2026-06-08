using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.UserProfile;
using FlowChat.ChatService.Application.Features.UserProfile.Commands.BulkUpsertOrDeleteUserProfileProjection;
using FlowChat.ChatService.Persistence.Entities;
using FlowChat.Shared.Persistance.BulkUpsert;

namespace FlowChat.ChatService.Persistence.BulkUpsert;

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
            [nameof(UserProfileReadModelEntity.UserId)],
            cancellationToken);

    protected override UserProfileReadModelEntity CreateUpsertEntity(
        UserProfileProjectionDto item,
        int sourceVersion,
        DateTimeOffset sourceCreatedAtUtc,
        DateTimeOffset sourceLastModifiedAtUtc,
        DateTimeOffset? sourceDeletedAtUtc,
        DateTimeOffset now) =>
        new()
        {
            UserId = item.UserProfileId,
            FriendlyUserId = item.FriendlyUserId,
            FirstName = item.FirstName,
            LastName = item.LastName,
            AvatarUrl = item.AvatarUrl,
            SourceVersion = sourceVersion,
            SourceCreatedAtUtc = sourceCreatedAtUtc,
            SourceLastModifiedAtUtc = sourceLastModifiedAtUtc,
            SourceDeletedAtUtc = sourceDeletedAtUtc ?? default,
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
            UserId = item.EntityId.Value,
            FriendlyUserId = string.Empty,
            SourceVersion = item.SourceVersion,
            SourceCreatedAtUtc = item.SourceCreatedAtUtc,
            SourceLastModifiedAtUtc = item.SourceLastModifiedAtUtc,
            SourceDeletedAtUtc = item.SourceDeletedAtUtc ?? now,
            CreatedBy = TombstoneSource,
            CreatedAtUtc = now,
            LastModifiedBy = TombstoneSource,
            LastModifiedAtUtc = now
        };
}
