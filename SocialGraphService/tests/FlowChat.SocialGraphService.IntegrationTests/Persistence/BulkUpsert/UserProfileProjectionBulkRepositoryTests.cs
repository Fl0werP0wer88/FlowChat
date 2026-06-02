using FlowChat.Shared.Domain;
using FlowChat.SocialGraphService.Application.Features.UserProfile;
using FlowChat.SocialGraphService.Application.Features.UserProfile.Commands.BulkUpsertOrDeleteUserProfileProjection;
using FlowChat.SocialGraphService.Persistence;
using FlowChat.SocialGraphService.Persistence.BulkUpsert;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.SocialGraphService.IntegrationTests.Persistence.BulkUpsert;

public sealed class UserProfileProjectionBulkRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _dbContext;
    private readonly UserProfileProjectionBulkRepository _repository;

    public UserProfileProjectionBulkRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new AppDbContext(options);
        _dbContext.Database.EnsureCreated();
        _repository = new UserProfileProjectionBulkRepository(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task BulkUpsertOrDeleteAsync_WhenProjectionDoesNotExist_InsertsProjection()
    {
        var userProfileId = Guid.NewGuid();

        await SaveAsync(CreateUpsertItem(userProfileId, 1, friendlyUserId: "jdoe", firstName: "John", avatarUrl: "https://avatar"));

        var entity = await _dbContext.UserProfileProjections.SingleAsync();
        entity.UserProfileId.Should().Be(userProfileId);
        entity.FirstName.Should().Be("John");
        entity.LastName.Should().BeNull();
        entity.AvatarUrl.Should().Be("https://avatar");
        entity.SourceVersion.Should().Be(1);
        entity.IsDeleted.Should().BeFalse();
        entity.CreatedBy.Should().Be("consumer");
        entity.CreatedAtUtc.Should().NotBe(default);
        entity.LastModifiedBy.Should().Be("consumer");
        entity.LastModifiedAtUtc.Should().NotBe(default);
    }

    [Fact]
    public async Task BulkUpsertOrDeleteAsync_WhenNewerProjectionExists_UpdatesProjection()
    {
        var userProfileId = Guid.NewGuid();
        await SaveAsync(CreateUpsertItem(userProfileId, 1, friendlyUserId: "jdoe", firstName: "Before"));
        var firstCreatedAtUtc = await _dbContext.UserProfileProjections
            .Where(x => x.UserProfileId == userProfileId)
            .Select(x => x.CreatedAtUtc)
            .SingleAsync();

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
        entity.IsDeleted.Should().BeFalse();
        entity.LastModifiedBy.Should().Be("updater-consumer");
        entity.LastModifiedAtUtc.Should().BeAfter(firstCreatedAtUtc);
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
        entity.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task BulkUpsertOrDeleteAsync_WhenDeleteDoesNotHaveExistingProjection_CreatesTombstone()
    {
        var userProfileId = Guid.NewGuid();

        await SaveAsync(CreateDeleteItem(userProfileId, 3));

        var entity = await _dbContext.UserProfileProjections.SingleAsync();
        entity.UserProfileId.Should().Be(userProfileId);
        entity.FriendlyUserId.Should().BeEmpty();
        entity.SourceVersion.Should().Be(3);
        entity.IsDeleted.Should().BeTrue();
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
        entity.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task BulkUpsertOrDeleteAsync_WhenUpsertIsOlderThanTombstone_DoesNotRestoreProjection()
    {
        var userProfileId = Guid.NewGuid();
        await SaveAsync(CreateDeleteItem(userProfileId, 3));

        await SaveAsync(CreateUpsertItem(userProfileId, 2, friendlyUserId: "stale", firstName: "Stale"));

        var entity = await _dbContext.UserProfileProjections.SingleAsync();
        entity.SourceVersion.Should().Be(3);
        entity.IsDeleted.Should().BeTrue();
    }

    private async Task SaveAsync(UserProfileProjectionCommandItem item)
    {
        await _repository.BulkUpsertOrSoftDeleteAsync([item], CancellationToken.None);
        await _dbContext.SaveChangesAsync();
    }

    private static UserProfileProjectionCommandItem CreateUpsertItem(
        Guid userProfileId,
        int sourceVersion,
        string friendlyUserId = "jdoe",
        string? firstName = "John",
        string? lastName = null,
        string? avatarUrl = null,
        string source = "consumer") =>
        new(
            Id<UserProfileProjectionDto>.FromGuid(userProfileId),
            new UserProfileProjectionDto
            {
                UserProfileId = userProfileId,
                FriendlyUserId = friendlyUserId,
                FirstName = firstName,
                LastName = lastName,
                AvatarUrl = avatarUrl,
                IsActive = true,
                SourceVersion = sourceVersion,
                Source = source
            },
            sourceVersion);

    private static UserProfileProjectionCommandItem CreateDeleteItem(Guid userProfileId, int sourceVersion) =>
        new(Id<UserProfileProjectionDto>.FromGuid(userProfileId), null, sourceVersion);
}
