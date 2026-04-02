using FlowChat.SocialGraphService.Application.Features.UserProfile;
using FlowChat.SocialGraphService.Persistence;
using FlowChat.SocialGraphService.Persistence.Entities;
using FlowChat.SocialGraphService.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.SocialGraphService.IntegrationTests.Persistence.Repositories;

public sealed class UserProfileReadModelRepositoryTests
{
    [Fact]
    public async Task UpsertAsync_WhenReadModelDoesNotExist_AddsNewEntityAndReturnsTrue()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CreateDbContext(connection);
        var repository = new UserProfileReadModelRepository(context);
        var readModel = new UserProfileReadModel(
            Guid.NewGuid(),
            "jdoe",
            "John Doe",
            "john@example.com",
            "+48123123123",
            "https://cdn.example/avatar.png",
            "Hello there",
            true,
            new DateTime(2026, 4, 1, 10, 30, 0, DateTimeKind.Utc),
            true,
            false);

        var wasCreated = await repository.UpsertAsync(readModel, CancellationToken.None);
        await context.SaveChangesAsync();

        var entity = await context.UserProfileReadModels.SingleAsync(x => x.UserProfileId == readModel.UserProfileId);

        wasCreated.Should().BeTrue();
        entity.UserName.Should().Be("jdoe");
        entity.DisplayName.Should().Be("John Doe");
        entity.MainEmail.Should().Be("john@example.com");
        entity.MainPhone.Should().Be("+48123123123");
        entity.AvatarUrl.Should().Be("https://cdn.example/avatar.png");
        entity.Bio.Should().Be("Hello there");
        entity.IsActive.Should().BeTrue();
        entity.LastSeenAtUtc.Should().Be(readModel.LastSeenAtUtc);
        entity.IsEmailVisible.Should().BeTrue();
        entity.IsPhoneVisible.Should().BeFalse();
        entity.CreatedBy.Should().Be("user-profile-events");
        entity.LastModifiedBy.Should().Be("user-profile-events");
    }

    [Fact]
    public async Task UpsertAsync_WhenReadModelExists_UpdatesEntityAndReturnsFalse()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var userProfileId = Guid.NewGuid();
        var originalLastModifiedAtUtc = new DateTimeOffset(2026, 3, 30, 8, 0, 0, TimeSpan.Zero);

        await using (var seedContext = CreateDbContext(connection))
        {
            seedContext.UserProfileReadModels.Add(new UserProfileReadModelEntity
            {
                UserProfileId = userProfileId,
                UserName = "old-user",
                DisplayName = "Old Display Name",
                MainEmail = "old@example.com",
                MainPhone = "+48000000000",
                AvatarUrl = "https://cdn.example/old.png",
                Bio = "Old bio",
                IsActive = false,
                LastSeenAtUtc = new DateTime(2026, 3, 29, 12, 0, 0, DateTimeKind.Utc),
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
        var repository = new UserProfileReadModelRepository(updateContext);
        var updatedReadModel = new UserProfileReadModel(
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

        var wasCreated = await repository.UpsertAsync(updatedReadModel, CancellationToken.None);
        await updateContext.SaveChangesAsync();

        var entity = await updateContext.UserProfileReadModels.SingleAsync(x => x.UserProfileId == userProfileId);

        wasCreated.Should().BeFalse();
        entity.UserName.Should().Be("new-user");
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
