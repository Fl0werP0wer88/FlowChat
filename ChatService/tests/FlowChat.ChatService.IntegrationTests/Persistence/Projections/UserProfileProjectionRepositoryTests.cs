using FlowChat.ChatService.Application.Features.UserProfile;
using FlowChat.ChatService.Persistence;
using FlowChat.ChatService.Persistence.Projections;
using FlowChat.Core.Messaging;
using FlowChat.Shared.Application;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace FlowChat.ChatService.IntegrationTests.Persistence.Projections;

public sealed class UserProfileProjectionRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private DbContextOptions<AppDbContext> _options = null!;
    private AppDbContext _dbContext = null!;
    private UserProfileProjectionRepository _repository = null!;

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;
        _dbContext = new AppDbContext(_options);
        await _dbContext.Database.EnsureCreatedAsync();
        _repository = new UserProfileProjectionRepository(_dbContext, TimeProvider.System);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }

    [Fact]
    public async Task UpsertOrSoftDeleteAsync_MissingProjection_InsertsProjection()
    {
        var userProfileId = Guid.NewGuid();

        await SaveAsync(CreateUpsertItem(userProfileId, 1, "jdoe", "John"));

        var entity = await _dbContext.UserProfileProjections.SingleAsync();
        entity.UserId.Should().Be(userProfileId);
        entity.FriendlyUserId.Should().Be("jdoe");
        entity.FirstName.Should().Be("John");
        entity.SourceVersion.Should().Be(1);
        entity.DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task UpsertOrSoftDeleteAsync_NewerProjection_UpdatesProjection()
    {
        var userProfileId = Guid.NewGuid();
        await SaveAsync(CreateUpsertItem(userProfileId, 1, "before", "Before"));

        await SaveAsync(CreateUpsertItem(userProfileId, 2, "after", "After"));

        var entity = await _dbContext.UserProfileProjections.SingleAsync();
        entity.FriendlyUserId.Should().Be("after");
        entity.FirstName.Should().Be("After");
        entity.SourceVersion.Should().Be(2);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task UpsertOrSoftDeleteAsync_IncomingVersionIsNotNewer_IgnoresProjection(int incomingVersion)
    {
        var userProfileId = Guid.NewGuid();
        await SaveAsync(CreateUpsertItem(userProfileId, 2, "current", "Current"));

        await SaveAsync(CreateUpsertItem(userProfileId, incomingVersion, "stale", "Stale"));

        var entity = await _dbContext.UserProfileProjections.SingleAsync();
        entity.FriendlyUserId.Should().Be("current");
        entity.FirstName.Should().Be("Current");
        entity.SourceVersion.Should().Be(2);
    }

    [Fact]
    public async Task UpsertOrSoftDeleteAsync_NewerDelete_CreatesTombstone()
    {
        var userProfileId = Guid.NewGuid();
        await SaveAsync(CreateUpsertItem(userProfileId, 1));

        await SaveAsync(CreateDeleteItem(userProfileId, 2));

        var entity = await _dbContext.UserProfileProjections.SingleAsync();
        entity.SourceVersion.Should().Be(2);
        entity.SourceDeletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task UpsertOrSoftDeleteAsync_DeleteWithoutTimestamp_UsesCurrentTime()
    {
        var item = CreateDeleteItem(Guid.NewGuid(), 1) with { SourceDeletedAtUtc = null };

        await SaveAsync(item);

        (await _dbContext.UserProfileProjections.SingleAsync()).SourceDeletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task UpsertOrSoftDeleteAsync_ConcurrentDuplicateInsert_StoresSingleVersion()
    {
        var userProfileId = Guid.NewGuid();
        await using var firstContext = new AppDbContext(_options);
        await using var secondContext = new AppDbContext(_options);
        var firstRepository = new UserProfileProjectionRepository(firstContext, TimeProvider.System);
        var secondRepository = new UserProfileProjectionRepository(secondContext, TimeProvider.System);

        await Task.WhenAll(
            firstRepository.UpsertOrSoftDeleteAsync(
                CreateUpsertItem(userProfileId, 1, "first"),
                CancellationToken.None),
            secondRepository.UpsertOrSoftDeleteAsync(
                CreateUpsertItem(userProfileId, 1, "second"),
                CancellationToken.None));

        _dbContext.ChangeTracker.Clear();
        var entity = await _dbContext.UserProfileProjections.SingleAsync();
        entity.SourceVersion.Should().Be(1);
        entity.FriendlyUserId.Should().BeOneOf("first", "second");
    }

    private async Task SaveAsync(ProjectionCommandItem<UserProfileProjectionDto> item)
    {
        await _repository.UpsertOrSoftDeleteAsync(item, CancellationToken.None);
        _dbContext.ChangeTracker.Clear();
    }

    private static ProjectionCommandItem<UserProfileProjectionDto> CreateUpsertItem(
        Guid userProfileId,
        int sourceVersion,
        string friendlyUserId = "jdoe",
        string? firstName = "John") =>
        new(
            new UserProfileProjectionDto
            {
                UserProfileId = userProfileId,
                FriendlyUserId = friendlyUserId,
                FirstName = firstName,
                SourceVersion = sourceVersion,
                Source = "consumer"
            },
            OperationType.Updated,
            sourceVersion,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            null);

    private static ProjectionCommandItem<UserProfileProjectionDto> CreateDeleteItem(Guid userProfileId, int sourceVersion) =>
        new(
            new UserProfileProjectionDto
            {
                UserProfileId = userProfileId,
                FriendlyUserId = string.Empty
            },
            OperationType.Deleted,
            sourceVersion,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);
}
