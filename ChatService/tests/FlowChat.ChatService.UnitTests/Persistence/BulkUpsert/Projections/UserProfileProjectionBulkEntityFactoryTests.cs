using FlowChat.ChatService.Application.Features.UserProfile;
using FlowChat.ChatService.Persistence.BulkUpsert.Projections;
using FlowChat.ChatService.Persistence.Entities;
using FlowChat.Core.Messaging;
using FlowChat.Shared.Application;
using FluentAssertions;

namespace FlowChat.ChatService.UnitTests.Persistence.BulkUpsert.Projections;

public sealed class UserProfileProjectionBulkEntityFactoryTests
{
    private readonly UserProfileProjectionBulkEntityFactory _factory = new();

    [Fact]
    public void UpdateByProperties_ReturnsUserIdColumn()
    {
        _factory.UpdateByProperties.Should().BeEquivalentTo([nameof(UserProfileReadModelEntity.UserId)]);
    }

    [Fact]
    public void CreateUpsertEntity_MapsAllFields()
    {
        var userProfileId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        var modifiedAt = DateTimeOffset.UtcNow;

        var entity = _factory.CreateUpsertEntity(
            new UserProfileProjectionDto
            {
                UserProfileId = userProfileId,
                FriendlyUserId = "jdoe",
                FirstName = "John",
                LastName = "Doe",
                AvatarUrl = "https://avatar",
                Source = "user-profile-projection"
            },
            sourceVersion: 3,
            sourceCreatedAtUtc: createdAt,
            sourceLastModifiedAtUtc: modifiedAt,
            sourceDeletedAtUtc: null);

        entity.UserId.Should().Be(userProfileId);
        entity.FriendlyUserId.Should().Be("jdoe");
        entity.FirstName.Should().Be("John");
        entity.LastName.Should().Be("Doe");
        entity.AvatarUrl.Should().Be("https://avatar");
        entity.SourceVersion.Should().Be(3);
        entity.SourceCreatedAtUtc.Should().Be(createdAt);
        entity.SourceLastModifiedAtUtc.Should().Be(modifiedAt);
        entity.SourceDeletedAtUtc.Should().BeNull();
    }

    [Fact]
    public void CreateTombstoneEntity_WhenSourceDeletedAtUtcIsNull_FallsBackToNow()
    {
        var userProfileId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var item = new ProjectionCommandItem<UserProfileProjectionDto>(
            new UserProfileProjectionDto
            {
                UserProfileId = userProfileId,
                FriendlyUserId = string.Empty
            },
            OperationType.Deleted,
            SourceVersion: 4,
            SourceCreatedAtUtc: now.AddMinutes(-10),
            SourceLastModifiedAtUtc: now.AddMinutes(-1),
            SourceDeletedAtUtc: null);

        var entity = _factory.CreateTombstoneEntity(item, now);

        entity.UserId.Should().Be(userProfileId);
        entity.FriendlyUserId.Should().BeEmpty();
        entity.SourceVersion.Should().Be(4);
        entity.SourceDeletedAtUtc.Should().Be(now);
    }
}
