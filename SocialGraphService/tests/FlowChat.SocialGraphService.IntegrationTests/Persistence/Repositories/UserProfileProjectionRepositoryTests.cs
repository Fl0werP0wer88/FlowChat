using FlowChat.SocialGraphService.Application.Features.UserProfile;
using FlowChat.SocialGraphService.Persistence;
using FlowChat.SocialGraphService.Persistence.Entities;
using FlowChat.SocialGraphService.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.SocialGraphService.IntegrationTests.Persistence.Repositories;

public sealed class UserProfileProjectionRepositoryTests
{
    [Fact]
    public async Task InsertAsync_WhenProjectionDoesNotExist_AddsNewEntityAndReturnsTrue()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CreateDbContext(connection);
        var repository = new UserProfileProjectionRepository(context);
        var projection = new UserProfileProjection(
            Guid.NewGuid(),
            "jdoe",
            "John Doe",
            "john@example.com",
            "+48123123123",
            "https://cdn.example/avatar.png",
            "Hello there",
            true,
            new DateTimeOffset(2026, 4, 1, 10, 30, 0, TimeSpan.Zero),
            true,
            false);

        var wasInserted = await repository.InsertAsync(projection, CancellationToken.None);
        await context.SaveChangesAsync();

        var entity = await context.UserProfileProjections.SingleAsync(x => x.UserProfileId == projection.UserProfileId);

        wasInserted.Should().BeTrue();
        entity.FriendlyUserId.Should().Be("jdoe");
        entity.DisplayName.Should().Be("John Doe");
        entity.MainEmail.Should().Be("john@example.com");
        entity.MainPhone.Should().Be("+48123123123");
        entity.AvatarUrl.Should().Be("https://cdn.example/avatar.png");
        entity.Bio.Should().Be("Hello there");
        entity.IsActive.Should().BeTrue();
        entity.LastSeenAtUtc.Should().Be(projection.LastSeenAtUtc);
        entity.IsEmailVisible.Should().BeTrue();
        entity.IsPhoneVisible.Should().BeFalse();
        entity.CreatedBy.Should().Be("user-profile-events");
        entity.LastModifiedBy.Should().Be("user-profile-events");
    }

    [Fact]
    public async Task InsertAsync_WhenProjectionExists_ReturnsFalseAndLeavesEntityUnchanged()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var userProfileId = Guid.NewGuid();

        await using (var seedContext = CreateDbContext(connection))
        {
            seedContext.UserProfileProjections.Add(new UserProfileProjectionEntity
            {
                UserProfileId = userProfileId,
                FriendlyUserId = "existing-user",
                DisplayName = "Existing Display Name",
                CreatedBy = "seed",
                CreatedAtUtc = new DateTimeOffset(2026, 3, 29, 7, 0, 0, TimeSpan.Zero),
                LastModifiedBy = "seed",
                LastModifiedAtUtc = new DateTimeOffset(2026, 3, 30, 8, 0, 0, TimeSpan.Zero)
            });

            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateDbContext(connection);
        var repository = new UserProfileProjectionRepository(context);
        var projection = new UserProfileProjection(
            userProfileId,
            "new-user",
            "New Display Name",
            null,
            null,
            null,
            null,
            true,
            null,
            false,
            false);

        var wasInserted = await repository.InsertAsync(projection, CancellationToken.None);
        await context.SaveChangesAsync();

        var entity = await context.UserProfileProjections.SingleAsync(x => x.UserProfileId == userProfileId);

        wasInserted.Should().BeFalse();
        entity.FriendlyUserId.Should().Be("existing-user");
        entity.DisplayName.Should().Be("Existing Display Name");
    }

    [Fact]
    public async Task UpdateAsync_WhenProjectionExists_UpdatesEntityAndReturnsTrue()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var userProfileId = Guid.NewGuid();
        var originalLastModifiedAtUtc = new DateTimeOffset(2026, 3, 30, 8, 0, 0, TimeSpan.Zero);

        await using (var seedContext = CreateDbContext(connection))
        {
            seedContext.UserProfileProjections.Add(new UserProfileProjectionEntity
            {
                UserProfileId = userProfileId,
                FriendlyUserId = "old-user",
                DisplayName = "Old Display Name",
                MainEmail = "old@example.com",
                MainPhone = "+48000000000",
                AvatarUrl = "https://cdn.example/old.png",
                Bio = "Old bio",
                IsActive = false,
                LastSeenAtUtc = new DateTimeOffset(2026, 3, 29, 12, 0, 0, TimeSpan.Zero),
                IsEmailVisible = false,
                IsPhoneVisible = false,
                CreatedBy = "seed",
                CreatedAtUtc = new DateTimeOffset(2026, 3, 29, 7, 0, 0, TimeSpan.Zero),
                LastModifiedBy = "seed",
                LastModifiedAtUtc = originalLastModifiedAtUtc
            });

            await seedContext.SaveChangesAsync();
        }

        await using var updateContext = CreateDbContext(connection);
        var repository = new UserProfileProjectionRepository(updateContext);
        var updatedProjection = new UserProfileProjection(
            userProfileId,
            "new-user",
            "New Display Name",
            null,
            "+48123123123",
            null,
            "Updated bio",
            true,
            null,
            true,
            true);

        var wasUpdated = await repository.UpdateAsync(updatedProjection, CancellationToken.None);
        await updateContext.SaveChangesAsync();

        var entity = await updateContext.UserProfileProjections.SingleAsync(x => x.UserProfileId == userProfileId);

        wasUpdated.Should().BeTrue();
        entity.FriendlyUserId.Should().Be("new-user");
        entity.DisplayName.Should().Be("New Display Name");
        entity.MainEmail.Should().BeNull();
        entity.MainPhone.Should().Be("+48123123123");
        entity.AvatarUrl.Should().BeNull();
        entity.Bio.Should().Be("Updated bio");
        entity.IsActive.Should().BeTrue();
        entity.LastSeenAtUtc.Should().BeNull();
        entity.IsEmailVisible.Should().BeTrue();
        entity.IsPhoneVisible.Should().BeTrue();
        entity.CreatedBy.Should().Be("seed");
        entity.LastModifiedBy.Should().Be("user-profile-events");
        entity.LastModifiedAtUtc.Should().BeAfter(originalLastModifiedAtUtc);
    }

    [Fact]
    public async Task UpdateAsync_WhenProjectionDoesNotExist_ReturnsFalse()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CreateDbContext(connection);
        var repository = new UserProfileProjectionRepository(context);
        var projection = new UserProfileProjection(
            Guid.NewGuid(),
            "jdoe",
            "John Doe",
            null,
            null,
            null,
            null,
            true,
            null,
            false,
            false);

        var wasUpdated = await repository.UpdateAsync(projection, CancellationToken.None);

        wasUpdated.Should().BeFalse();
    }

    private static AppDbContext CreateDbContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
