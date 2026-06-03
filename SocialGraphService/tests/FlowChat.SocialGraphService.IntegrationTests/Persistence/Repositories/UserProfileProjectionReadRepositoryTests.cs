using FlowChat.SocialGraphService.Persistence;
using FlowChat.SocialGraphService.Persistence.Entities;
using FlowChat.SocialGraphService.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.SocialGraphService.IntegrationTests.Persistence.Repositories;

public sealed class UserProfileProjectionReadRepositoryTests
{
    [Fact]
    public async Task GetByUserProfileIdAsync_WhenProjectionExists_ReturnsProjection()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var userProfileId = Guid.NewGuid();

        await using (var seedContext = CreateDbContext(connection))
        {
            seedContext.UserProfileProjections.Add(
                CreateProjection("jdoe", "Jane", "Doe", "FlowChat", userProfileId, "jane@example.com"));
            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(connection);
        var repository = new UserProfileProjectionReadRepository(readContext);

        var result = await repository.GetByUserProfileIdAsync(userProfileId, CancellationToken.None);

        result.Should().NotBeNull();
        result!.UserProfileId.Should().Be(userProfileId);
        result.FirstName.Should().Be("Jane");
        result.LastName.Should().Be("Doe");
    }

    [Fact]
    public async Task GetByFriendlyUserIdAsync_WhenProjectionExists_ReturnsProjection()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using (var seedContext = CreateDbContext(connection))
        {
            seedContext.UserProfileProjections.Add(
                CreateProjection("jdoe", "Jane", "Doe", "FlowChat", Guid.NewGuid(), "jane@example.com"));
            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(connection);
        var repository = new UserProfileProjectionReadRepository(readContext);

        var result = await repository.GetByFriendlyUserIdAsync("jdoe", CancellationToken.None);

        result.Should().NotBeNull();
        result!.FriendlyUserId.Should().Be("jdoe");
    }

    [Fact]
    public async Task GetByEmailAsync_WhenProjectionExists_ReturnsProjectionUsingCaseInsensitiveMatch()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using (var seedContext = CreateDbContext(connection))
        {
            seedContext.UserProfileProjections.Add(
                CreateProjection("jdoe", "Jane", "Doe", "FlowChat", Guid.NewGuid(), "Jane@Example.com"));
            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(connection);
        var repository = new UserProfileProjectionReadRepository(readContext);

        var result = await repository.GetByEmailAsync("jane@example.com", CancellationToken.None);

        result.Should().NotBeNull();
        result!.MainEmail.Should().NotBeNull();
        result.MainEmail!.Address.Should().Be("Jane@Example.com");
    }

    [Fact]
    public async Task GetByUserProfileIdAsync_WhenProjectionIsDeleted_ReturnsNull()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var userProfileId = Guid.NewGuid();

        await using (var seedContext = CreateDbContext(connection))
        {
            seedContext.UserProfileProjections.Add(
                CreateProjection("jdoe", "Jane", "Doe", "FlowChat", userProfileId, "jane@example.com", isDeleted: true));
            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(connection);
        var repository = new UserProfileProjectionReadRepository(readContext);

        var result = await repository.GetByUserProfileIdAsync(userProfileId, CancellationToken.None);

        result.Should().BeNull();
    }

    private static UserProfileReadModelEntity CreateProjection(
        string friendlyUserId,
        string? firstName,
        string? lastName,
        string? organization,
        Guid? userProfileId = null,
        string? mainEmail = null,
        bool isDeleted = false) =>
        new()
        {
            UserProfileId = userProfileId ?? Guid.NewGuid(),
            FriendlyUserId = friendlyUserId,
            FirstName = firstName,
            LastName = lastName,
            Organization = organization,
            MainEmail = mainEmail,
            MainEmailIsConfirmed = mainEmail == null ? null : false,
            MainEmailIsVisible = mainEmail == null ? null : true,
            IsActive = true,
            SourceVersion = 1,
            DeletedAt = isDeleted ? new DateTimeOffset(2026, 4, 24, 12, 0, 0, TimeSpan.Zero) : null,
            CreatedBy = "seed",
            CreatedAtUtc = new DateTimeOffset(2026, 4, 6, 8, 0, 0, TimeSpan.Zero),
            LastModifiedBy = "seed",
            LastModifiedAtUtc = new DateTimeOffset(2026, 4, 6, 8, 0, 0, TimeSpan.Zero)
        };

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

