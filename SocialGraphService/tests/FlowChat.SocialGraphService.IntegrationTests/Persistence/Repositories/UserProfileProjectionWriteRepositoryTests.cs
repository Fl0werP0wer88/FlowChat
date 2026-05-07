using FlowChat.SocialGraphService.Application.Features.UserProfile;
using FlowChat.SocialGraphService.Persistence;
using FlowChat.SocialGraphService.Persistence.Entities;
using FlowChat.SocialGraphService.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.SocialGraphService.IntegrationTests.Persistence.Repositories;

public sealed class UserProfileProjectionWriteRepositoryTests
{
    [Fact]
    public async Task InsertAsync_WhenProjectionDoesNotExist_AddsNewEntity()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CreateDbContext(connection);
        var repository = new UserProfileProjectionWriteRepository(context);
        var projection = new UserProfileProjectionDto
        {
            UserProfileId = Guid.NewGuid(),
            FriendlyUserId = "jdoe",
            FirstName = "John",
            LastName = "Doe",
            Organization = "FlowChat",
            MainEmail = new UserProfileProjectionEmailDto
            {
                Address = "john@example.com",
                IsConfirmed = true,
                IsVisible = true
            },
            MainPhone = new UserProfileProjectionPhoneDto
            {
                Number = "+48123123123",
                IsConfirmed = false,
                IsVisible = true
            },
            AvatarUrl = "https://cdn.example/avatar.png",
            Bio = "Hello there",
            IsActive = true,
            LastSeenAtUtc = new DateTimeOffset(2026, 4, 1, 10, 30, 0, TimeSpan.Zero)
        };

        await repository.InsertAsync(projection, CancellationToken.None);
        await context.SaveChangesAsync();

        var entity = await context.UserProfileProjections.SingleAsync(x => x.UserProfileId == projection.UserProfileId);

        entity.FriendlyUserId.Should().Be("jdoe");
        entity.FirstName.Should().Be("John");
        entity.LastName.Should().Be("Doe");
        entity.Organization.Should().Be("FlowChat");
        entity.MainEmail.Should().Be("john@example.com");
        entity.MainEmailIsConfirmed.Should().BeTrue();
        entity.MainEmailIsVisible.Should().BeTrue();
        entity.MainPhone.Should().Be("+48123123123");
        entity.MainPhoneIsConfirmed.Should().BeFalse();
        entity.MainPhoneIsVisible.Should().BeTrue();
        entity.AvatarUrl.Should().Be("https://cdn.example/avatar.png");
        entity.Bio.Should().Be("Hello there");
        entity.IsActive.Should().BeTrue();
        entity.LastSeenAtUtc.Should().Be(projection.LastSeenAtUtc);
        entity.CreatedBy.Should().Be("user-profile-events");
        entity.LastModifiedBy.Should().Be("user-profile-events");
    }

    [Fact]
    public async Task ExistsAsync_WhenProjectionExists_ReturnsTrue()
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
                CreatedBy = "seed",
                CreatedAtUtc = new DateTimeOffset(2026, 3, 29, 7, 0, 0, TimeSpan.Zero),
                LastModifiedBy = "seed",
                LastModifiedAtUtc = new DateTimeOffset(2026, 3, 30, 8, 0, 0, TimeSpan.Zero)
            });

            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateDbContext(connection);
        var repository = new UserProfileProjectionWriteRepository(context);

        var exists = await repository.ExistsAsync(userProfileId, CancellationToken.None);

        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenProjectionDoesNotExist_ReturnsFalse()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CreateDbContext(connection);
        var repository = new UserProfileProjectionWriteRepository(context);

        var exists = await repository.ExistsAsync(Guid.NewGuid(), CancellationToken.None);

        exists.Should().BeFalse();
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
                MainEmail = "old@example.com",
                MainEmailIsConfirmed = false,
                MainEmailIsVisible = true,
                MainPhone = "+48000000000",
                MainPhoneIsConfirmed = false,
                MainPhoneIsVisible = false,
                AvatarUrl = "https://cdn.example/old.png",
                Bio = "Old bio",
                IsActive = false,
                LastSeenAtUtc = new DateTimeOffset(2026, 3, 29, 12, 0, 0, TimeSpan.Zero),
                CreatedBy = "seed",
                CreatedAtUtc = new DateTimeOffset(2026, 3, 29, 7, 0, 0, TimeSpan.Zero),
                LastModifiedBy = "seed",
                LastModifiedAtUtc = originalLastModifiedAtUtc
            });

            await seedContext.SaveChangesAsync();
        }

        await using var updateContext = CreateDbContext(connection);
        var repository = new UserProfileProjectionWriteRepository(updateContext);
        var updatedProjection = new UserProfileProjectionDto
        {
            UserProfileId = userProfileId,
            FriendlyUserId = "new-user",
            FirstName = "Jane",
            LastName = "Doe",
            Organization = "FlowChat",
            MainPhone = new UserProfileProjectionPhoneDto
            {
                Number = "+48123123123",
                IsConfirmed = true,
                IsVisible = true
            },
            Bio = "Updated bio",
            IsActive = true
        };

        var wasUpdated = await repository.UpdateAsync(updatedProjection, CancellationToken.None);
        await updateContext.SaveChangesAsync();

        var entity = await updateContext.UserProfileProjections.SingleAsync(x => x.UserProfileId == userProfileId);

        wasUpdated.Should().BeTrue();
        entity.FriendlyUserId.Should().Be("new-user");
        entity.FirstName.Should().Be("Jane");
        entity.LastName.Should().Be("Doe");
        entity.Organization.Should().Be("FlowChat");
        entity.MainEmail.Should().BeNull();
        entity.MainEmailIsConfirmed.Should().BeNull();
        entity.MainEmailIsVisible.Should().BeNull();
        entity.MainPhone.Should().Be("+48123123123");
        entity.MainPhoneIsConfirmed.Should().BeTrue();
        entity.MainPhoneIsVisible.Should().BeTrue();
        entity.AvatarUrl.Should().BeNull();
        entity.Bio.Should().Be("Updated bio");
        entity.IsActive.Should().BeTrue();
        entity.LastSeenAtUtc.Should().BeNull();
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
        var repository = new UserProfileProjectionWriteRepository(context);
        var projection = new UserProfileProjectionDto
        {
            UserProfileId = Guid.NewGuid(),
            FriendlyUserId = "jdoe",
            IsActive = true
        };

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


