using FlowChat.Shared.Application;
using FlowChat.Shared.Persistance.ProjectionBulk;
using FlowChat.SocialGraphService.Application.Features.UserProfile;
using FlowChat.SocialGraphService.Persistence.Entities;

namespace FlowChat.SocialGraphService.Persistence.BulkUpsert.Projections;

public sealed class UserProfileProjectionBulkEntityFactory
    : IProjectionBulkEntityFactory<UserProfileProjectionDto, UserProfileReadModelEntity>
{
    public IReadOnlyList<string> UpdateByProperties { get; } = [nameof(UserProfileReadModelEntity.UserProfileId)];

    public UserProfileReadModelEntity CreateUpsertEntity(
        UserProfileProjectionDto value,
        int sourceVersion,
        DateTimeOffset sourceCreatedAtUtc,
        DateTimeOffset sourceLastModifiedAtUtc,
        DateTimeOffset? sourceDeletedAtUtc) =>
        new()
        {
            UserProfileId = value.UserProfileId,
            FriendlyUserId = value.FriendlyUserId,
            FirstName = value.FirstName,
            LastName = value.LastName,
            Organization = value.Organization,
            MainEmail = value.MainEmail?.Address,
            MainEmailIsConfirmed = value.MainEmail?.IsConfirmed,
            MainEmailIsVisible = value.MainEmail?.IsVisible,
            MainPhone = value.MainPhone?.Number,
            MainPhoneIsConfirmed = value.MainPhone?.IsConfirmed,
            MainPhoneIsVisible = value.MainPhone?.IsVisible,
            AvatarUrl = value.AvatarUrl,
            Bio = value.Bio,
            IsActive = value.IsActive,
            LastSeenAtUtc = value.LastSeenAtUtc,
            SourceVersion = sourceVersion,
            SourceCreatedAtUtc = sourceCreatedAtUtc,
            SourceLastModifiedAtUtc = sourceLastModifiedAtUtc,
            SourceDeletedAtUtc = sourceDeletedAtUtc
        };

    public UserProfileReadModelEntity CreateTombstoneEntity(
        ProjectionCommandItem<UserProfileProjectionDto> item,
        DateTimeOffset now) =>
        new()
        {
            UserProfileId = item.Value.UserProfileId,
            FriendlyUserId = item.Value.FriendlyUserId,
            IsActive = false,
            SourceVersion = item.SourceVersion,
            SourceCreatedAtUtc = item.SourceCreatedAtUtc,
            SourceLastModifiedAtUtc = item.SourceLastModifiedAtUtc,
            SourceDeletedAtUtc = item.SourceDeletedAtUtc ?? now
        };
}
