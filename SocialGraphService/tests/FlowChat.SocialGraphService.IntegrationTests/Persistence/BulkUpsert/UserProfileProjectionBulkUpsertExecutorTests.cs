using FlowChat.SocialGraphService.Application.Features.UserProfile;
using FlowChat.SocialGraphService.Persistence;
using FlowChat.SocialGraphService.Persistence.BulkUpsert;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.SocialGraphService.IntegrationTests.Persistence.BulkUpsert;

public sealed class UserProfileProjectionBulkUpsertExecutorTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _dbContext;
    private readonly UserProfileProjectionBulkUpsertExecutor _executor;

    public UserProfileProjectionBulkUpsertExecutorTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new AppDbContext(options);
        _dbContext.Database.EnsureCreated();
        _executor = new UserProfileProjectionBulkUpsertExecutor(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task UpsertAsync_WhenProjectionExists_UpdatesProjection()
    {
        var userProfileId = Guid.NewGuid();
        await _executor.UpsertAsync(
            [new UserProfileProjectionDto { UserProfileId = userProfileId, FriendlyUserId = "jdoe", FirstName = "Before", IsActive = true }],
            CancellationToken.None);
        var createdAtUtc = await _dbContext.UserProfileProjections
            .Where(x => x.UserProfileId == userProfileId)
            .Select(x => x.CreatedAtUtc)
            .SingleAsync();

        await Task.Delay(10);
        await _executor.UpsertAsync(
            [new UserProfileProjectionDto { UserProfileId = userProfileId, FriendlyUserId = "jdoe", FirstName = "After", IsActive = true }],
            CancellationToken.None);

        var entity = await _dbContext.UserProfileProjections.SingleAsync();
        entity.FirstName.Should().Be("After");
        entity.CreatedAtUtc.Should().BeAfter(createdAtUtc);
        entity.LastModifiedAtUtc.Should().BeAfter(createdAtUtc);
    }
}
