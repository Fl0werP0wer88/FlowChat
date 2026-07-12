using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.PresenceService.Application.Features.ContactObserverProjections;
using FlowChat.PresenceService.Persistence;
using FlowChat.PresenceService.Persistence.BulkUpsert.Projections;
using FlowChat.PresenceService.Persistence.Entities;
using FlowChat.Shared.Application;
using FlowChat.Shared.Persistance.ProjectionBulk;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace FlowChat.PresenceService.IntegrationTests.Persistence.BulkUpsert;

// EFCore.BulkExtensions generates Postgres-specific SQL (ON CONFLICT ... WHERE) that SQLite cannot model,
// so this needs a real Postgres instance rather than the SQLite in-memory provider used elsewhere.
public sealed class ContactObserverProjectionBulkRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private AppDbContext _dbContext = null!;
    private ProjectionBulkRepository<AppDbContext, ContactObserverProjectionDto, ContactObserverReadModelEntity, ContactObserverProjectionBulkEntityFactory> _repository = null!;

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;

        _dbContext = new AppDbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();
        _repository = new ProjectionBulkRepository<AppDbContext, ContactObserverProjectionDto, ContactObserverReadModelEntity, ContactObserverProjectionBulkEntityFactory>(
            _dbContext,
            new ContactObserverProjectionBulkEntityFactory());
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _postgresContainer.DisposeAsync();
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
    }

    [Fact]
    public async Task BulkUpsertOrDeleteAsync_WhenNewerProjectionExists_UpdatesProjection()
    {
        var observedUserId = Guid.NewGuid();
        var observerUserId = Guid.NewGuid();
        await SaveAsync(CreateUpsertItem(observedUserId, observerUserId, 1));

        await Task.Delay(10);
        await SaveAsync(CreateUpsertItem(observedUserId, observerUserId, 2, "updater-consumer"));

        var entity = await _dbContext.ContactObserverProjections.SingleAsync();
        entity.SourceVersion.Should().Be(2);
        entity.DeletedAt.Should().BeNull();
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

    [Fact]
    public async Task BulkUpsertOrDeleteAsync_WhenItemViolatesCheckConstraint_ThrowsIsolableException()
    {
        var userId = Guid.NewGuid();

        var act = () => SaveAsync(CreateUpsertItem(userId, userId, 1));

        await act.Should().ThrowAsync<IsolableException>();
    }

    private async Task SaveAsync(ProjectionCommandItem<ContactObserverProjectionDto> item)
    {
        // BulkUpsertOrSoftDeleteAsync uses UseTempDB internally (EFCore.BulkExtensions), which requires
        // an explicit transaction so the temp table survives until the operation completes; in production
        // this is provided by the surrounding command handler's unit-of-work transaction.
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        await _repository.BulkUpsertOrSoftDeleteAsync([item], CancellationToken.None);
        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();
        _dbContext.ChangeTracker.Clear();
    }

    private static ProjectionCommandItem<ContactObserverProjectionDto> CreateUpsertItem(
        Guid observedUserId,
        Guid observerUserId,
        int sourceVersion,
        string source = "consumer") =>
        new(
            new ContactObserverProjectionDto
            {
                ObservedUserId = observedUserId,
                ObserverUserId = observerUserId,
                SourceVersion = sourceVersion,
                Source = source
            },
            OperationType.Updated,
            sourceVersion,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            null);

    private static ProjectionCommandItem<ContactObserverProjectionDto> CreateDeleteItem(
        Guid observedUserId,
        Guid observerUserId,
        int sourceVersion) =>
        new(
            new ContactObserverProjectionDto
            {
                ObservedUserId = observedUserId,
                ObserverUserId = observerUserId
            },
            OperationType.Deleted,
            sourceVersion,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);
}
