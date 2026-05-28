using FlowChat.ChatService.Application.Features.UserProfile;
using FlowChat.ChatService.Persistence;
using FlowChat.ChatService.Persistence.BulkUpsert;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.ChatService.IntegrationTests.Persistence.BulkUpsert;

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
    public async Task UpsertAsync_WhenProjectionDoesNotExist_InsertsProjection()
    {
        var userProfileId = Guid.NewGuid();
        var result = await _executor.UpsertAsync(
            [
                new UserProfileProjectionDto
                {
                    UserProfileId = userProfileId,
                    FriendlyUserId = "jdoe",
                    FirstName = "John",
                    AvatarUrl = "https://avatar",
                    Source = "consumer"
                }
            ],
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var entity = await _dbContext.UserProfileProjections.SingleAsync();
        entity.UserId.Should().Be(userProfileId);
        entity.FirstName.Should().Be("John");
        entity.LastName.Should().BeNull();
        entity.CreatedBy.Should().Be("consumer");
        entity.CreatedAtUtc.Should().NotBe(default);
        entity.LastModifiedBy.Should().Be("consumer");
        entity.LastModifiedAtUtc.Should().NotBe(default);
    }

    [Fact]
    public async Task UpsertAsync_WhenProjectionExists_UpdatesProjection()
    {
        var userProfileId = Guid.NewGuid();
        await _executor.UpsertAsync(
            [
                new UserProfileProjectionDto
                {
                    UserProfileId = userProfileId,
                    FriendlyUserId = "jdoe",
                    FirstName = "Before",
                    Source = "initial-consumer"
                }
            ],
            CancellationToken.None);
        var firstCreatedAtUtc = await _dbContext.UserProfileProjections
            .Where(x => x.UserId == userProfileId)
            .Select(x => x.CreatedAtUtc)
            .SingleAsync();

        await Task.Delay(10);
        await _executor.UpsertAsync(
            [
                new UserProfileProjectionDto
                {
                    UserProfileId = userProfileId,
                    FriendlyUserId = "jdoe2",
                    FirstName = "After",
                    LastName = "Updated",
                    Source = "updater-consumer"
                }
            ],
            CancellationToken.None);

        var entity = await _dbContext.UserProfileProjections.SingleAsync();
        entity.FriendlyUserId.Should().Be("jdoe2");
        entity.FirstName.Should().Be("After");
        entity.LastName.Should().Be("Updated");
        entity.LastModifiedBy.Should().Be("updater-consumer");
        entity.LastModifiedAtUtc.Should().BeAfter(firstCreatedAtUtc);
    }
}
