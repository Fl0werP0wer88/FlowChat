using FlowChat.ChatService.Application.Features.UserProfile;
using FlowChat.ChatService.Persistence.Entities;
using FlowChat.Shared.Application;
using FlowChat.Shared.Persistance.ProjectionBulk;

namespace FlowChat.ChatService.Persistence.BulkUpsert.Projections;

public sealed class UserProfileProjectionBulkEntityFactory
    : IProjectionBulkEntityFactory<UserProfileProjectionDto, UserProfileReadModelEntity>
{
    public IReadOnlyList<string> UpdateByProperties { get; } = [nameof(UserProfileReadModelEntity.UserId)];

    public UserProfileReadModelEntity CreateUpsertEntity(
        UserProfileProjectionDto value,
        int sourceVersion,
        DateTimeOffset sourceCreatedAtUtc,
        DateTimeOffset sourceLastModifiedAtUtc,
        DateTimeOffset? sourceDeletedAtUtc) =>
        new()
        {
            UserId = value.UserProfileId,
            FriendlyUserId = value.FriendlyUserId,
            FirstName = value.FirstName,
            LastName = value.LastName,
            AvatarUrl = value.AvatarUrl,
            Email = value.Email,
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
            UserId = item.Value.UserProfileId,
            FriendlyUserId = item.Value.FriendlyUserId,
            SourceVersion = item.SourceVersion,
            SourceCreatedAtUtc = item.SourceCreatedAtUtc,
            SourceLastModifiedAtUtc = item.SourceLastModifiedAtUtc,
            SourceDeletedAtUtc = item.SourceDeletedAtUtc ?? now
        };
}
