using FlowChat.SocialGraphService.Persistence;
using FlowChat.SocialGraphService.Persistence.Entities;
using FlowChat.SocialGraphService.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.SocialGraphService.IntegrationTests.Persistence.Repositories;

public sealed class UserProfileProjectionReadRepositoryTests
{
    [Fact]
    public async Task SearchAsync_WhenProjectionsMatchAllPrefixes_ReturnsOnlyMatchingRowsUsingAndLogic()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using (var seedContext = CreateDbContext(connection))
        {
            seedContext.UserProfileProjections.AddRange(
                CreateProjection("jdoe", "Jane Doe", "Jane", "Doe", "FlowChat"),
                CreateProjection("jdoe2", "Janet Doe", "Janet", "Doe", "FlowLab"),
                CreateProjection("jwrong-last", "Jane Smith", "Jane", "Smith", "FlowChat"),
                CreateProjection("jwrong-org", "Jane Doe Other", "Jane", "Doe", "OtherCorp"),
                CreateProjection("jwrong-first", "Ann Doe", "Ann", "Doe", "FlowChat"),
                CreateProjection("jnull-org", "Jane Doe Null", "Jane", "Doe", null));

            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(connection);
        var repository = new UserProfileProjectionReadRepository(readContext);

        var result = await repository.SearchAsync("Jan", "Do", "Flow", CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(projection => projection.DisplayName).Should().Equal("Jane Doe", "Janet Doe");
        result.All(projection =>
            projection.FirstName!.StartsWith("Jan", StringComparison.Ordinal) &&
            projection.LastName!.StartsWith("Do", StringComparison.Ordinal) &&
            projection.Organization!.StartsWith("Flow", StringComparison.Ordinal)).Should().BeTrue();
    }

    [Fact]
    public async Task SearchAsync_WhenNothingMatches_ReturnsEmptyList()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using (var seedContext = CreateDbContext(connection))
        {
            seedContext.UserProfileProjections.Add(CreateProjection("jdoe", "Jane Doe", "Jane", "Doe", "FlowChat"));
            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(connection);
        var repository = new UserProfileProjectionReadRepository(readContext);

        var result = await repository.SearchAsync("Mark", "Tw", "Acme", CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_WhenOnlyFirstNameIsProvided_ReturnsMatchesForThatSingleCriterion()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using (var seedContext = CreateDbContext(connection))
        {
            seedContext.UserProfileProjections.AddRange(
                CreateProjection("jdoe", "Jane Doe", "Jane", "Doe", "FlowChat"),
                CreateProjection("jsmith", "Jane Smith", "Jane", "Smith", "OtherCorp"),
                CreateProjection("adoe", "Ann Doe", "Ann", "Doe", "FlowChat"));

            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(connection);
        var repository = new UserProfileProjectionReadRepository(readContext);

        var result = await repository.SearchAsync("Jan", null, string.Empty, CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(projection => projection.DisplayName).Should().Equal("Jane Doe", "Jane Smith");
    }

    [Fact]
    public async Task SearchAsync_WhenCriterionIsNullOrEmpty_IgnoresThatFilter()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using (var seedContext = CreateDbContext(connection))
        {
            seedContext.UserProfileProjections.AddRange(
                CreateProjection("jdoe", "Jane Doe", "Jane", "Doe", "FlowChat"),
                CreateProjection("jdoe2", "Jane Doe Flow", "Jane", "Doe", "FlowLab"),
                CreateProjection("jother", "Jane Other", "Jane", "Other", "FlowChat"));

            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(connection);
        var repository = new UserProfileProjectionReadRepository(readContext);

        var result = await repository.SearchAsync(null, "Do", "Flow", CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(projection => projection.DisplayName).Should().Equal("Jane Doe", "Jane Doe Flow");
    }

    [Fact]
    public async Task GetByUserProfileIdAsync_WhenProjectionExists_ReturnsProjection()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var userProfileId = Guid.NewGuid();

        await using (var seedContext = CreateDbContext(connection))
        {
            seedContext.UserProfileProjections.Add(
                CreateProjection("jdoe", "Jane Doe", "Jane", "Doe", "FlowChat", userProfileId, "jane@example.com"));
            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(connection);
        var repository = new UserProfileProjectionReadRepository(readContext);

        var result = await repository.GetByUserProfileIdAsync(userProfileId, CancellationToken.None);

        result.Should().NotBeNull();
        result!.UserProfileId.Should().Be(userProfileId);
        result.DisplayName.Should().Be("Jane Doe");
    }

    [Fact]
    public async Task GetByFriendlyUserIdAsync_WhenProjectionExists_ReturnsProjection()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using (var seedContext = CreateDbContext(connection))
        {
            seedContext.UserProfileProjections.Add(
                CreateProjection("jdoe", "Jane Doe", "Jane", "Doe", "FlowChat", Guid.NewGuid(), "jane@example.com"));
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
                CreateProjection("jdoe", "Jane Doe", "Jane", "Doe", "FlowChat", Guid.NewGuid(), "Jane@Example.com"));
            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(connection);
        var repository = new UserProfileProjectionReadRepository(readContext);

        var result = await repository.GetByEmailAsync("jane@example.com", CancellationToken.None);

        result.Should().NotBeNull();
        result!.MainEmail.Should().Be("Jane@Example.com");
    }

    private static UserProfileProjectionEntity CreateProjection(
        string friendlyUserId,
        string displayName,
        string? firstName,
        string? lastName,
        string? organization,
        Guid? userProfileId = null,
        string? mainEmail = null) =>
        new()
        {
            UserProfileId = userProfileId ?? Guid.NewGuid(),
            FriendlyUserId = friendlyUserId,
            DisplayName = displayName,
            FirstName = firstName,
            LastName = lastName,
            Organization = organization,
            MainEmail = mainEmail,
            IsActive = true,
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
