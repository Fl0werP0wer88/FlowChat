using FlowChat.ChatService.Application.Features.UserProfile;
using FlowChat.ChatService.Persistence;
using FlowChat.ChatService.Persistence.BulkUpsert.Projections;
using FlowChat.ChatService.Persistence.Entities;
using FlowChat.Core.Messaging;
using FlowChat.Shared.Application;
using FlowChat.Shared.Persistance.ProjectionBulk;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace FlowChat.ChatService.IntegrationTests.Persistence.BulkUpsert;

// EFCore.BulkExtensions generates Postgres-specific SQL (ON CONFLICT ... WHERE) that SQLite cannot model,
// so this needs a real Postgres instance rather than the SQLite in-memory provider used elsewhere.
public sealed class UserProfileProjectionBulkRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private AppDbContext _dbContext = null!;
    private ProjectionBulkRepository<AppDbContext, UserProfileProjectionDto, UserProfileReadModelEntity, UserProfileProjectionBulkEntityFactory> _repository = null!;

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;

        _dbContext = new AppDbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();
        _repository = new ProjectionBulkRepository<AppDbContext, UserProfileProjectionDto, UserProfileReadModelEntity, UserProfileProjectionBulkEntityFactory>(
            _dbContext,
            new UserProfileProjectionBulkEntityFactory());
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }

    [Fact]
    public async Task BulkUpsertOrDeleteAsync_WhenProjectionDoesNotExist_InsertsProjection()
    {
        var userProfileId = Guid.NewGuid();

        await SaveAsync(CreateUpsertItem(userProfileId, 1, friendlyUserId: "jdoe", firstName: "John", avatarUrl: "https://avatar"));

        var entity = await _dbContext.UserProfileProjections.SingleAsync();
        entity.UserId.Should().Be(userProfileId);
        entity.FirstName.Should().Be("John");
        entity.LastName.Should().BeNull();
        entity.SourceVersion.Should().Be(1);
        entity.DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task BulkUpsertOrDeleteAsync_WhenNewerProjectionExists_UpdatesProjection()
    {
        var userProfileId = Guid.NewGuid();
        await SaveAsync(CreateUpsertItem(userProfileId, 1, friendlyUserId: "jdoe", firstName: "Before"));

        await Task.Delay(10);
        await SaveAsync(CreateUpsertItem(
            userProfileId,
            2,
            friendlyUserId: "jdoe2",
            firstName: "After",
            lastName: "Updated",
            source: "updater-consumer"));

        var entity = await _dbContext.UserProfileProjections.SingleAsync();
        entity.FriendlyUserId.Should().Be("jdoe2");
        entity.FirstName.Should().Be("After");
        entity.LastName.Should().Be("Updated");
        entity.SourceVersion.Should().Be(2);
        entity.DeletedAt.Should().BeNull();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task BulkUpsertOrDeleteAsync_WhenIncomingVersionIsNotNewer_IgnoresProjection(int incomingVersion)
    {
        var userProfileId = Guid.NewGuid();
        await SaveAsync(CreateUpsertItem(userProfileId, 2, friendlyUserId: "current", firstName: "Current"));

        await SaveAsync(CreateUpsertItem(userProfileId, incomingVersion, friendlyUserId: "stale", firstName: "Stale"));

        var entity = await _dbContext.UserProfileProjections.SingleAsync();
        entity.FriendlyUserId.Should().Be("current");
        entity.FirstName.Should().Be("Current");
        entity.SourceVersion.Should().Be(2);
    }

    [Fact]
    public async Task BulkUpsertOrDeleteAsync_WhenDeleteIsNewer_MarksProjectionAsDeleted()
    {
        var userProfileId = Guid.NewGuid();
        await SaveAsync(CreateUpsertItem(userProfileId, 1));

        await SaveAsync(CreateDeleteItem(userProfileId, 2));

        var entity = await _dbContext.UserProfileProjections.SingleAsync();
        entity.SourceVersion.Should().Be(2);
        entity.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task BulkUpsertOrDeleteAsync_WhenDeleteDoesNotHaveExistingProjection_CreatesTombstone()
    {
        var userProfileId = Guid.NewGuid();

        await SaveAsync(CreateDeleteItem(userProfileId, 3));

        var entity = await _dbContext.UserProfileProjections.SingleAsync();
        entity.UserId.Should().Be(userProfileId);
        entity.FriendlyUserId.Should().BeEmpty();
        entity.SourceVersion.Should().Be(3);
        entity.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task BulkUpsertOrDeleteAsync_WhenUpsertIsNewerThanTombstone_RestoresProjection()
    {
        var userProfileId = Guid.NewGuid();
        await SaveAsync(CreateDeleteItem(userProfileId, 3));

        await SaveAsync(CreateUpsertItem(userProfileId, 4, friendlyUserId: "restored", firstName: "Restored"));

        var entity = await _dbContext.UserProfileProjections.SingleAsync();
        entity.FriendlyUserId.Should().Be("restored");
        entity.FirstName.Should().Be("Restored");
        entity.SourceVersion.Should().Be(4);
        entity.DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task BulkUpsertOrDeleteAsync_WhenUpsertIsOlderThanTombstone_DoesNotRestoreProjection()
    {
        var userProfileId = Guid.NewGuid();
        await SaveAsync(CreateDeleteItem(userProfileId, 3));

        await SaveAsync(CreateUpsertItem(userProfileId, 2, friendlyUserId: "stale", firstName: "Stale"));

        var entity = await _dbContext.UserProfileProjections.SingleAsync();
        entity.SourceVersion.Should().Be(3);
        entity.DeletedAt.Should().NotBeNull();
    }

    private async Task SaveAsync(ProjectionCommandItem<UserProfileProjectionDto> item)
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

    private static ProjectionCommandItem<UserProfileProjectionDto> CreateUpsertItem(
        Guid userProfileId,
        int sourceVersion,
        string friendlyUserId = "jdoe",
        string? firstName = "John",
        string? lastName = null,
        string? avatarUrl = null,
        string source = "consumer") =>
        new(
            new UserProfileProjectionDto
            {
                UserProfileId = userProfileId,
                FriendlyUserId = friendlyUserId,
                FirstName = firstName,
                LastName = lastName,
                AvatarUrl = avatarUrl,
                SourceVersion = sourceVersion,
                Source = source
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
