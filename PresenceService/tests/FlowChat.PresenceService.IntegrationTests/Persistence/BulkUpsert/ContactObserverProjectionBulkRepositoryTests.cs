using FlowChat.PresenceService.Application.Features.ContactObserverProjections;
using FlowChat.PresenceService.Application.Features.ContactObserverProjections.Commands.BulkUpsertOrDeleteUserContactProjection;
using FlowChat.PresenceService.Persistence;
using FlowChat.PresenceService.Persistence.BulkUpsert;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.PresenceService.IntegrationTests.Persistence.BulkUpsert;

public sealed class ContactObserverProjectionBulkRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _dbContext;
    private readonly ContactObserverProjectionBulkRepository _repository;

    public ContactObserverProjectionBulkRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new AppDbContext(options);
        _dbContext.Database.EnsureCreated();
        _repository = new ContactObserverProjectionBulkRepository(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task BulkUpsertOrDeleteAsync_WhenProjectionDoesNotExist_InsertsProjection()
    {
        var observedUserId = Guid.NewGuid();
        var observerUserId = Guid.NewGuid();

        await SaveAsync(CreateUpsertItem(observedUserId, observerUserId, 1));

        var entity = await _dbContext.ContactObserverProjections.SingleAsync();
        entity.ObservedUserId.Should().Be(observedUserId);
        entity.ObserverUserId.Should().Be(observerUserId);
        entity.SourceVersion.Should().Be(1);
        entity.DeletedAt.Should().BeNull();
        entity.CreatedBy.Should().Be("consumer");
        entity.CreatedAtUtc.Should().NotBe(default);
        entity.LastModifiedBy.Should().Be("consumer");
        entity.LastModifiedAtUtc.Should().NotBe(default);
    }

    [Fact]
    public async Task BulkUpsertOrDeleteAsync_WhenNewerProjectionExists_UpdatesProjection()
    {
        var observedUserId = Guid.NewGuid();
        var observerUserId = Guid.NewGuid();
        await SaveAsync(CreateUpsertItem(observedUserId, observerUserId, 1));
        var firstCreatedAtUtc = await _dbContext.ContactObserverProjections
            .Where(x => x.ObservedUserId == observedUserId && x.ObserverUserId == observerUserId)
            .Select(x => x.CreatedAtUtc)
            .SingleAsync();

        await Task.Delay(10);
        await SaveAsync(CreateUpsertItem(observedUserId, observerUserId, 2, "updater-consumer"));

        var entity = await _dbContext.ContactObserverProjections.SingleAsync();
        entity.SourceVersion.Should().Be(2);
        entity.DeletedAt.Should().BeNull();
        entity.LastModifiedBy.Should().Be("updater-consumer");
        entity.LastModifiedAtUtc.Should().BeAfter(firstCreatedAtUtc);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task BulkUpsertOrDeleteAsync_WhenIncomingVersionIsNotNewer_IgnoresProjection(int incomingVersion)
    {
        var observedUserId = Guid.NewGuid();
        var observerUserId = Guid.NewGuid();
        await SaveAsync(CreateUpsertItem(observedUserId, observerUserId, 2));

        await SaveAsync(CreateUpsertItem(observedUserId, observerUserId, incomingVersion, "stale-consumer"));

        var entity = await _dbContext.ContactObserverProjections.SingleAsync();
        entity.SourceVersion.Should().Be(2);
        entity.LastModifiedBy.Should().Be("consumer");
    }

    [Fact]
    public async Task BulkUpsertOrDeleteAsync_WhenDeleteIsNewer_MarksProjectionAsDeleted()
    {
        var observedUserId = Guid.NewGuid();
        var observerUserId = Guid.NewGuid();
        await SaveAsync(CreateUpsertItem(observedUserId, observerUserId, 1));

        await SaveAsync(CreateDeleteItem(observedUserId, observerUserId, 2));

        var entity = await _dbContext.ContactObserverProjections.SingleAsync();
        entity.SourceVersion.Should().Be(2);
        entity.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task BulkUpsertOrDeleteAsync_WhenDeleteDoesNotHaveExistingProjection_CreatesTombstone()
    {
        var observedUserId = Guid.NewGuid();
        var observerUserId = Guid.NewGuid();

        await SaveAsync(CreateDeleteItem(observedUserId, observerUserId, 3));

        var entity = await _dbContext.ContactObserverProjections.SingleAsync();
        entity.ObservedUserId.Should().Be(observedUserId);
        entity.ObserverUserId.Should().Be(observerUserId);
        entity.SourceVersion.Should().Be(3);
        entity.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task BulkUpsertOrDeleteAsync_WhenUpsertIsNewerThanTombstone_RestoresProjection()
    {
        var observedUserId = Guid.NewGuid();
        var observerUserId = Guid.NewGuid();
        await SaveAsync(CreateDeleteItem(observedUserId, observerUserId, 3));

        await SaveAsync(CreateUpsertItem(observedUserId, observerUserId, 4));

        var entity = await _dbContext.ContactObserverProjections.SingleAsync();
        entity.SourceVersion.Should().Be(4);
        entity.DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task BulkUpsertOrDeleteAsync_WhenUpsertIsOlderThanTombstone_DoesNotRestoreProjection()
    {
        var observedUserId = Guid.NewGuid();
        var observerUserId = Guid.NewGuid();
        await SaveAsync(CreateDeleteItem(observedUserId, observerUserId, 3));

        await SaveAsync(CreateUpsertItem(observedUserId, observerUserId, 2));

        var entity = await _dbContext.ContactObserverProjections.SingleAsync();
        entity.SourceVersion.Should().Be(3);
        entity.DeletedAt.Should().NotBeNull();
    }

    private async Task SaveAsync(UserContactProjectionCommandItem item)
    {
        await _repository.BulkUpsertOrSoftDeleteAsync([item], CancellationToken.None);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();
    }

    private static UserContactProjectionCommandItem CreateUpsertItem(
        Guid observedUserId,
        Guid observerUserId,
        int sourceVersion,
        string source = "consumer") =>
        new(
            observedUserId,
            observerUserId,
            new ContactObserverProjectionDto
            {
                ObservedUserId = observedUserId,
                ObserverUserId = observerUserId,
                SourceVersion = sourceVersion,
                Source = source
            },
            sourceVersion,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            null);

    private static UserContactProjectionCommandItem CreateDeleteItem(
        Guid observedUserId,
        Guid observerUserId,
        int sourceVersion) =>
        new(
            observedUserId,
            observerUserId,
            null,
            sourceVersion,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);
}
