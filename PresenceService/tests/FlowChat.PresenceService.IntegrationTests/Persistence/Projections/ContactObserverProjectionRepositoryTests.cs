using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.PresenceService.Application.Features.ContactObserverProjections;
using FlowChat.PresenceService.Persistence;
using FlowChat.PresenceService.Persistence.Projections;
using FlowChat.Shared.Application;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace FlowChat.PresenceService.IntegrationTests.Persistence.Projections;

public sealed class ContactObserverProjectionRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private AppDbContext _dbContext = null!;
    private DbContextOptions<AppDbContext> _options = null!;
    private ContactObserverProjectionRepository _repository = null!;

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;

        _dbContext = new AppDbContext(_options);
        await _dbContext.Database.EnsureCreatedAsync();
        _repository = new ContactObserverProjectionRepository(_dbContext);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }

    [Fact]
    public async Task UpsertOrSoftDeleteAsync_WhenProjectionDoesNotExist_InsertsProjection()
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
    public async Task UpsertOrSoftDeleteAsync_WhenNewerProjectionExists_UpdatesProjection()
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
    public async Task UpsertOrSoftDeleteAsync_WhenIncomingVersionIsNotNewer_IgnoresProjection(int incomingVersion)
    {
        var observedUserId = Guid.NewGuid();
        var observerUserId = Guid.NewGuid();
        await SaveAsync(CreateUpsertItem(observedUserId, observerUserId, 2));

        await SaveAsync(CreateUpsertItem(observedUserId, observerUserId, incomingVersion, "stale-consumer"));

        var entity = await _dbContext.ContactObserverProjections.SingleAsync();
        entity.SourceVersion.Should().Be(2);
    }

    [Fact]
    public async Task UpsertOrSoftDeleteAsync_WhenDeleteIsNewer_MarksProjectionAsDeleted()
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
    public async Task UpsertOrSoftDeleteAsync_WhenDeleteDoesNotHaveExistingProjection_CreatesTombstone()
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
    public async Task UpsertOrSoftDeleteAsync_WhenUpsertIsNewerThanTombstone_RestoresProjection()
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
    public async Task UpsertOrSoftDeleteAsync_WhenUpsertIsOlderThanTombstone_DoesNotRestoreProjection()
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
    public async Task UpsertOrSoftDeleteAsync_WhenItemViolatesCheckConstraint_ThrowsIsolableException()
    {
        var userId = Guid.NewGuid();

        var act = () => SaveAsync(CreateUpsertItem(userId, userId, 1));

        await act.Should().ThrowAsync<IsolableException>();
    }

    [Fact]
    public async Task UpsertOrSoftDeleteAsync_WhenConcurrentDuplicateArrives_StoresSingleProjection()
    {
        var observedUserId = Guid.NewGuid();
        var observerUserId = Guid.NewGuid();
        await using var firstContext = new AppDbContext(_options);
        await using var secondContext = new AppDbContext(_options);
        var firstRepository = new ContactObserverProjectionRepository(firstContext);
        var secondRepository = new ContactObserverProjectionRepository(secondContext);

        await Task.WhenAll(
            firstRepository.UpsertOrSoftDeleteAsync(
                CreateUpsertItem(observedUserId, observerUserId, 1),
                CancellationToken.None),
            secondRepository.UpsertOrSoftDeleteAsync(
                CreateUpsertItem(observedUserId, observerUserId, 1),
                CancellationToken.None));

        _dbContext.ChangeTracker.Clear();
        var entity = await _dbContext.ContactObserverProjections.SingleAsync();
        entity.SourceVersion.Should().Be(1);
    }

    private async Task SaveAsync(ProjectionCommandItem<ContactObserverProjectionDto> item)
    {
        await _repository.UpsertOrSoftDeleteAsync(item, CancellationToken.None);
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
