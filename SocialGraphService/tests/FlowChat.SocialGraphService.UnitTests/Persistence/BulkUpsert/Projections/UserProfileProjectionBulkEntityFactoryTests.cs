using FlowChat.Core.Messaging;
using FlowChat.Shared.Application;
using FlowChat.SocialGraphService.Application.Features.UserProfile;
using FlowChat.SocialGraphService.Persistence.BulkUpsert.Projections;
using FlowChat.SocialGraphService.Persistence.Entities;
using FluentAssertions;

namespace FlowChat.SocialGraphService.UnitTests.Persistence.BulkUpsert.Projections;

public sealed class UserProfileProjectionBulkEntityFactoryTests
{
    private readonly UserProfileProjectionBulkEntityFactory _factory = new();

    [Fact]
    public void UpdateByProperties_ReturnsUserProfileIdColumn()
    {
        _factory.UpdateByProperties.Should().BeEquivalentTo([nameof(UserProfileReadModelEntity.UserProfileId)]);
    }

    [Fact]
    public void CreateUpsertEntity_MapsNestedEmailAndPhone()
    {
        var userProfileId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        var modifiedAt = DateTimeOffset.UtcNow;

        var entity = _factory.CreateUpsertEntity(
            new UserProfileProjectionDto
            {
                UserProfileId = userProfileId,
                FriendlyUserId = "jdoe",
                MainEmail = new UserProfileProjectionEmailDto { Address = "john@example.com", IsConfirmed = true, IsVisible = false },
                MainPhone = new UserProfileProjectionPhoneDto { Number = "+48123123123", IsConfirmed = false, IsVisible = true },
                IsActive = true,
                Source = "user-profile-projection"
            },
            sourceVersion: 3,
            sourceCreatedAtUtc: createdAt,
            sourceLastModifiedAtUtc: modifiedAt,
            sourceDeletedAtUtc: null);

        entity.UserProfileId.Should().Be(userProfileId);
        entity.MainEmail.Should().Be("john@example.com");
        entity.MainEmailIsConfirmed.Should().BeTrue();
        entity.MainEmailIsVisible.Should().BeFalse();
        entity.MainPhone.Should().Be("+48123123123");
        entity.MainPhoneIsConfirmed.Should().BeFalse();
        entity.MainPhoneIsVisible.Should().BeTrue();
        entity.IsActive.Should().BeTrue();
        entity.SourceVersion.Should().Be(3);
    }

    [Fact]
    public void CreateUpsertEntity_WhenEmailAndPhoneAreNull_LeavesColumnsNull()
    {
        var entity = _factory.CreateUpsertEntity(
            new UserProfileProjectionDto
            {
                UserProfileId = Guid.NewGuid(),
                FriendlyUserId = "jdoe"
            },
            sourceVersion: 1,
            sourceCreatedAtUtc: DateTimeOffset.UtcNow,
            sourceLastModifiedAtUtc: DateTimeOffset.UtcNow,
            sourceDeletedAtUtc: null);

        entity.MainEmail.Should().BeNull();
        entity.MainEmailIsConfirmed.Should().BeNull();
        entity.MainPhone.Should().BeNull();
        entity.MainPhoneIsConfirmed.Should().BeNull();
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

        entity.UserProfileId.Should().Be(userProfileId);
        entity.FriendlyUserId.Should().BeEmpty();
        entity.IsActive.Should().BeFalse();
        entity.SourceVersion.Should().Be(4);
        entity.SourceDeletedAtUtc.Should().Be(now);
    }
}
