using FlowChat.PresenceService.Application.Features.ContactObserverProjections;
using FlowChat.PresenceService.Persistence;
using FlowChat.PresenceService.Persistence.BulkUpsert;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.PresenceService.IntegrationTests.Persistence.BulkUpsert;

public sealed class ContactObserverProjectionBulkUpsertExecutorTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _dbContext;
    private readonly ContactObserverProjectionBulkUpsertExecutor _executor;

    public ContactObserverProjectionBulkUpsertExecutorTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new AppDbContext(options);
        _dbContext.Database.EnsureCreated();
        _executor = new ContactObserverProjectionBulkUpsertExecutor(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task UpsertAsync_WhenProjectionExists_UpdatesProjection()
    {
        var observedUserId = Guid.NewGuid();
        var observerUserId = Guid.NewGuid();
        var createdAtUtc = DateTimeOffset.UtcNow.AddMinutes(-5);
        await _executor.UpsertAsync(
            [
                new ContactObserverProjectionDto
                {
                    ObservedUserId = observedUserId,
                    ObserverUserId = observerUserId,
                    CreatedAtUtc = createdAtUtc,
                    LastModifiedAtUtc = createdAtUtc
                }
            ],
            CancellationToken.None);

        var updatedAtUtc = DateTimeOffset.UtcNow;
        await _executor.UpsertAsync(
            [
                new ContactObserverProjectionDto
                {
                    ObservedUserId = observedUserId,
                    ObserverUserId = observerUserId,
                    CreatedAtUtc = updatedAtUtc,
                    LastModifiedAtUtc = updatedAtUtc
                }
            ],
            CancellationToken.None);

        var entity = await _dbContext.ContactObserverProjections.SingleAsync();
        entity.CreatedAtUtc.Should().Be(updatedAtUtc);
        entity.LastModifiedAtUtc.Should().Be(updatedAtUtc);
    }
}
