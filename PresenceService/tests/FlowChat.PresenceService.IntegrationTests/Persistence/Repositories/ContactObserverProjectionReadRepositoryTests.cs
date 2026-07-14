using FlowChat.PresenceService.Persistence;
using FlowChat.PresenceService.Persistence.Entities;
using FlowChat.PresenceService.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.PresenceService.IntegrationTests.Persistence.Repositories;

public sealed class ContactObserverProjectionReadRepositoryTests
{
    [Fact]
    public async Task GetNonBlockedObserverUserIdsAsync_WhenProjectionIsDeleted_DoesNotReturnDeletedObserver()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var observedUserId = Guid.NewGuid();
        var activeObserverUserId = Guid.NewGuid();
        var deletedObserverUserId = Guid.NewGuid();

        await using (var seedContext = CreateDbContext(connection))
        {
            seedContext.ContactObserverProjections.AddRange(
                CreateProjection(observedUserId, activeObserverUserId),
                CreateProjection(
                    observedUserId,
                    deletedObserverUserId,
                    new DateTimeOffset(2026, 4, 24, 12, 0, 0, TimeSpan.Zero)));

            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(connection);
        var repository = new ContactObserverProjectionReadRepository(readContext);

        var result = await repository.GetNonBlockedObserverUserIdsAsync(observedUserId, CancellationToken.None);

        result.Should().ContainSingle().Which.Should().Be(activeObserverUserId);
    }

    [Fact]
    public async Task GetNonBlockedObservedUserIdsAsync_WhenProjectionIsDeleted_DoesNotReturnDeletedObservedUser()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var observerUserId = Guid.NewGuid();
        var activeObservedUserId = Guid.NewGuid();
        var deletedObservedUserId = Guid.NewGuid();

        await using (var seedContext = CreateDbContext(connection))
        {
            seedContext.ContactObserverProjections.AddRange(
                CreateProjection(activeObservedUserId, observerUserId),
                CreateProjection(
                    deletedObservedUserId,
                    observerUserId,
                    new DateTimeOffset(2026, 4, 24, 12, 0, 0, TimeSpan.Zero)));

            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(connection);
        var repository = new ContactObserverProjectionReadRepository(readContext);

        var result = await repository.GetNonBlockedObservedUserIdsAsync(observerUserId, CancellationToken.None);

        result.Should().ContainSingle().Which.Should().Be(activeObservedUserId);
    }

    [Fact]
    public async Task GetNonBlockedObserverUserIdsAsync_WhenObserverHasBlockedObserved_DoesNotReturnBlockedObserver()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var observedUserId = Guid.NewGuid();
        var activeObserverUserId = Guid.NewGuid();
        var blockedObserverUserId = Guid.NewGuid();

        await using (var seedContext = CreateDbContext(connection))
        {
            seedContext.ContactObserverProjections.AddRange(
                CreateProjection(observedUserId, activeObserverUserId),
                CreateProjection(observedUserId, blockedObserverUserId, isBlocked: true));

            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(connection);
        var repository = new ContactObserverProjectionReadRepository(readContext);

        var result = await repository.GetNonBlockedObserverUserIdsAsync(observedUserId, CancellationToken.None);

        result.Should().ContainSingle().Which.Should().Be(activeObserverUserId);
    }

    private static ContactObserverReadModelEntity CreateProjection(
        Guid observedUserId,
        Guid observerUserId,
        DateTimeOffset? deletedAt = null,
        bool isBlocked = false) =>
        new()
        {
            ObservedUserId = observedUserId,
            ObserverUserId = observerUserId,
            IsBlocked = isBlocked,
            SourceDeletedAtUtc = deletedAt
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
