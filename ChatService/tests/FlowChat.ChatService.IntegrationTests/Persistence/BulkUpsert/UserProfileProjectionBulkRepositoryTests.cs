using FlowChat.ChatService.Application.Features.UserProfile;
using FlowChat.ChatService.Application.Features.UserProfile.Commands.BulkUpsertOrDeleteUserProfileProjection;
using FlowChat.ChatService.Persistence;
using FlowChat.ChatService.Persistence.BulkUpsert;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.ChatService.IntegrationTests.Persistence.BulkUpsert;

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

        var result = await _repository.BulkUpsertOrDeleteAsync(
            [CreateUpsertItem(userProfileId, 1, friendlyUserId: "jdoe", firstName: "John", avatarUrl: "https://avatar")],
            CancellationToken.None);
        await _dbContext.SaveChangesAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(Unit.Value);

        var entity = await _dbContext.UserProfileProjections.SingleAsync();
        entity.UserId.Should().Be(userProfileId);
        entity.FirstName.Should().Be("John");
        entity.LastName.Should().BeNull();
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
            .Where(x => x.UserId == userProfileId)
            .Select(x => x.CreatedAtUtc)
            .SingleAsync();

        await Task.Delay(10);
        var result = await SaveAsync(CreateUpsertItem(
            userProfileId,
            2,
            friendlyUserId: "jdoe2",
            firstName: "After",
            lastName: "Updated",
            source: "updater-consumer"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(Unit.Value);

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

        var result = await SaveAsync(CreateUpsertItem(userProfileId, incomingVersion, friendlyUserId: "stale", firstName: "Stale"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(Unit.Value);
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

        var result = await SaveAsync(CreateDeleteItem(userProfileId, 2));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(Unit.Value);
        var entity = await _dbContext.UserProfileProjections.SingleAsync();
        entity.SourceVersion.Should().Be(2);
        entity.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task BulkUpsertOrDeleteAsync_WhenDeleteDoesNotHaveExistingProjection_CreatesTombstone()
    {
        var userProfileId = Guid.NewGuid();

        var result = await SaveAsync(CreateDeleteItem(userProfileId, 3));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(Unit.Value);
        var entity = await _dbContext.UserProfileProjections.SingleAsync();
        entity.UserId.Should().Be(userProfileId);
        entity.FriendlyUserId.Should().BeEmpty();
        entity.SourceVersion.Should().Be(3);
        entity.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task BulkUpsertOrDeleteAsync_WhenUpsertIsNewerThanTombstone_RestoresProjection()
    {
        var userProfileId = Guid.NewGuid();
        await SaveAsync(CreateDeleteItem(userProfileId, 3));

        var result = await SaveAsync(CreateUpsertItem(userProfileId, 4, friendlyUserId: "restored", firstName: "Restored"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(Unit.Value);
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

        var result = await SaveAsync(CreateUpsertItem(userProfileId, 2, friendlyUserId: "stale", firstName: "Stale"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(Unit.Value);
        var entity = await _dbContext.UserProfileProjections.SingleAsync();
        entity.SourceVersion.Should().Be(3);
        entity.IsDeleted.Should().BeTrue();
    }

    private async Task<FlowChatResult<Unit>> SaveAsync(UserProfileProjectionCommandItem item)
    {
        var result = await _repository.BulkUpsertOrDeleteAsync([item], CancellationToken.None);
        await _dbContext.SaveChangesAsync();
        return result;
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
                SourceVersion = sourceVersion,
                Source = source
            },
            sourceVersion);

    private static UserProfileProjectionCommandItem CreateDeleteItem(Guid userProfileId, int sourceVersion) =>
        new(Id<UserProfileProjectionDto>.FromGuid(userProfileId), null, sourceVersion);
}
