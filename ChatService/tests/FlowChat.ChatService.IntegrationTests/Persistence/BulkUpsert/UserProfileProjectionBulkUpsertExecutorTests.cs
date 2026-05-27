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
                    DisplayName = "John",
                    AvatarUrl = "https://avatar"
                }
            ],
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var entity = await _dbContext.UserProfileProjections.SingleAsync();
        entity.UserId.Should().Be(userProfileId);
        entity.DisplayName.Should().Be("John");
        entity.CreatedBy.Should().Be("user-profile-events");
        entity.CreatedAtUtc.Should().NotBe(default);
        entity.LastModifiedBy.Should().Be("user-profile-events");
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
                    DisplayName = "Before"
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
                    DisplayName = "After"
                }
            ],
            CancellationToken.None);

        var entity = await _dbContext.UserProfileProjections.SingleAsync();
        entity.FriendlyUserId.Should().Be("jdoe2");
        entity.DisplayName.Should().Be("After");
        entity.LastModifiedAtUtc.Should().BeAfter(firstCreatedAtUtc);
    }
}
