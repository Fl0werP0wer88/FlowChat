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

    private static UserProfileProjectionEntity CreateProjection(
        string friendlyUserId,
        string displayName,
        string? firstName,
        string? lastName,
        string? organization) =>
        new()
        {
            UserProfileId = Guid.NewGuid(),
            FriendlyUserId = friendlyUserId,
            DisplayName = displayName,
            FirstName = firstName,
            LastName = lastName,
            Organization = organization,
            IsActive = true,
            IsEmailVisible = false,
            IsPhoneVisible = false,
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
