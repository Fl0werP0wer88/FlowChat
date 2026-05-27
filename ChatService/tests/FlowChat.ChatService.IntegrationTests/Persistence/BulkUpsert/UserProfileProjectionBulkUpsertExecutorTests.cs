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
        var now = DateTimeOffset.UtcNow;

        var result = await _executor.UpsertAsync(
            [
                new UserProfileProjectionDto
                {
                    UserProfileId = userProfileId,
                    FriendlyUserId = "jdoe",
                    DisplayName = "John",
                    AvatarUrl = "https://avatar",
                    CreatedBy = "source",
                    CreatedAtUtc = now,
                    LastModifiedBy = "source",
                    LastModifiedAtUtc = now
                }
            ],
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var entity = await _dbContext.UserProfileProjections.SingleAsync();
        entity.UserId.Should().Be(userProfileId);
        entity.DisplayName.Should().Be("John");
        entity.CreatedAtUtc.Should().Be(now);
        entity.LastModifiedAtUtc.Should().Be(now);
    }

    [Fact]
    public async Task UpsertAsync_WhenProjectionExists_UpdatesProjection()
    {
        var userProfileId = Guid.NewGuid();
        var createdAtUtc = DateTimeOffset.UtcNow.AddMinutes(-5);
        var updatedAtUtc = DateTimeOffset.UtcNow;
        await _executor.UpsertAsync(
            [
                new UserProfileProjectionDto
                {
                    UserProfileId = userProfileId,
                    FriendlyUserId = "jdoe",
                    DisplayName = "Before",
                    CreatedBy = "source",
                    CreatedAtUtc = createdAtUtc,
                    LastModifiedBy = "source",
                    LastModifiedAtUtc = createdAtUtc
                }
            ],
            CancellationToken.None);

        await _executor.UpsertAsync(
            [
                new UserProfileProjectionDto
                {
                    UserProfileId = userProfileId,
                    FriendlyUserId = "jdoe2",
                    DisplayName = "After",
                    CreatedBy = "source",
                    CreatedAtUtc = updatedAtUtc,
                    LastModifiedBy = "source",
                    LastModifiedAtUtc = updatedAtUtc
                }
            ],
            CancellationToken.None);

        var entity = await _dbContext.UserProfileProjections.SingleAsync();
        entity.FriendlyUserId.Should().Be("jdoe2");
        entity.DisplayName.Should().Be("After");
        entity.CreatedAtUtc.Should().Be(updatedAtUtc);
        entity.LastModifiedAtUtc.Should().Be(updatedAtUtc);
    }
}
