using AutoFixture;
using FlowChat.NotificationService.Domain.Enums;
using FlowChat.NotificationService.Persistence;
using FlowChat.NotificationService.Persistence.Entities;
using FlowChat.NotificationService.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.NotificationService.IntegrationTests.Persistence.Repositories;

public sealed class NotificationReadRepositoryTests : IDisposable
{
    private readonly IFixture _fixture = new Fixture();
    private readonly AppDbContext _dbContext;
    private readonly NotificationReadRepository _repository;

    public NotificationReadRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(_fixture.Create<Guid>().ToString("N"))
            .Options;

        _dbContext = new AppDbContext(options);
        _repository = new NotificationReadRepository(_dbContext);
    }

    public void Dispose() => _dbContext.Dispose();

    private async Task<NotificationReadEntity> SeedNotificationAsync(
        Guid? userId = null,
        NotificationType type = NotificationType.EmailVerification,
        string? sourceMessageKey = null,
        DateTimeOffset? deletedAt = null)
    {
        var notification = new NotificationReadEntity
        {
            Id = _fixture.Create<Guid>(),
            UserId = userId ?? _fixture.Create<Guid>(),
            Email = $"{_fixture.Create<string>()}@example.com",
            DisplayName = _fixture.Create<string>(),
            Body = _fixture.Create<string>(),
            Type = type,
            Status = NotificationStatus.Pending,
            SourceMessageKey = sourceMessageKey,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            DeletedAt = deletedAt
        };

        _dbContext.NotificationReads.Add(notification);
        await _dbContext.SaveChangesAsync();
        return notification;
    }

    // --- ExistsByUserIdAndTypeAsync ---

    [Fact]
    public async Task ExistsByUserIdAndTypeAsync_WhenNotificationExists_ReturnsTrue()
    {
        var userId = _fixture.Create<Guid>();
        await SeedNotificationAsync(userId, NotificationType.EmailVerification);

        var result = await _repository.ExistsByUserIdAndTypeAsync(userId, NotificationType.EmailVerification);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsByUserIdAndTypeAsync_WhenNotificationDoesNotExist_ReturnsFalse()
    {
        var result = await _repository.ExistsByUserIdAndTypeAsync(_fixture.Create<Guid>(), NotificationType.EmailVerification);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsByUserIdAndTypeAsync_WhenUserExistsButTypeDiffers_ReturnsFalse()
    {
        var userId = _fixture.Create<Guid>();
        await SeedNotificationAsync(userId, NotificationType.EmailVerification);

        var result = await _repository.ExistsByUserIdAndTypeAsync(userId, NotificationType.Welcome);

        result.Should().BeFalse();
    }

    // --- ExistsBySourceMessageKeyAsync ---

    [Fact]
    public async Task ExistsBySourceMessageKeyAsync_WhenKeyExists_ReturnsTrue()
    {
        await SeedNotificationAsync(sourceMessageKey: "unique-key-1");

        var result = await _repository.ExistsBySourceMessageKeyAsync("unique-key-1");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsBySourceMessageKeyAsync_WhenKeyDoesNotExist_ReturnsFalse()
    {
        var result = await _repository.ExistsBySourceMessageKeyAsync("non-existent-key");

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExistsBySourceMessageKeyAsync_WhenKeyIsBlank_ReturnsFalse(string key)
    {
        var result = await _repository.ExistsBySourceMessageKeyAsync(key);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsBySourceMessageKeyAsync_TrimsKeyBeforeQuerying()
    {
        await SeedNotificationAsync(sourceMessageKey: "trimmed-key");

        var result = await _repository.ExistsBySourceMessageKeyAsync("  trimmed-key  ");

        result.Should().BeTrue();
    }

    // --- GetByUserIdAsync ---

    [Fact]
    public async Task GetByUserIdAsync_WhenNotificationsExist_ReturnsNotificationsForUser()
    {
        var userId = _fixture.Create<Guid>();
        await SeedNotificationAsync(userId);
        await SeedNotificationAsync(userId);
        await SeedNotificationAsync(); // different user

        var result = await _repository.GetByUserIdAsync(userId);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(n => n.UserId == userId);
    }

    [Fact]
    public async Task GetByUserIdAsync_WhenNoNotificationsExist_ReturnsEmptyList()
    {
        var result = await _repository.GetByUserIdAsync(_fixture.Create<Guid>());

        result.Should().BeEmpty();
    }

    // --- GetRecentAsync ---

    [Fact]
    public async Task GetRecentAsync_ReturnsAllNotifications()
    {
        await SeedNotificationAsync();
        await SeedNotificationAsync();

        var result = await _repository.GetRecentAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetRecentAsync_WhenNotificationIsDeleted_DoesNotReturnDeletedNotification()
    {
        await SeedNotificationAsync();
        await SeedNotificationAsync(deletedAt: new DateTimeOffset(2026, 4, 24, 12, 0, 0, TimeSpan.Zero));

        var result = await _repository.GetRecentAsync();

        result.Should().ContainSingle();
    }

    [Fact]
    public async Task GetRecentAsync_WhenNoNotifications_ReturnsEmptyList()
    {
        var result = await _repository.GetRecentAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRecentAsync_MapsAllExpectedFields()
    {
        var userId = _fixture.Create<Guid>();
        var notification = new NotificationReadEntity
        {
            Id = _fixture.Create<Guid>(),
            UserId = userId,
            Email = "test@example.com",
            DisplayName = "Test User",
            Body = "Confirm your email by clicking the provided link",
            Type = NotificationType.EmailVerification,
            Status = NotificationStatus.Sent,
            ProviderMessageId = "provider-msg-123",
            SourceMessageKey = "map-test-key",
            SentAtUtc = DateTimeOffset.UtcNow,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        _dbContext.NotificationReads.Add(notification);
        await _dbContext.SaveChangesAsync();

        var result = await _repository.GetRecentAsync();

        var dto = result.Should().ContainSingle().Subject;
        dto.UserId.Should().Be(userId);
        dto.Email.Should().Be("test@example.com");
        dto.DisplayName.Should().Be("Test User");
        dto.Body.Should().Be("Confirm your email by clicking the provided link");
        dto.Type.Should().Be(NotificationType.EmailVerification);
        dto.Status.Should().Be(NotificationStatus.Sent);
        dto.ProviderMessageId.Should().Be("provider-msg-123");
        dto.SourceMessageKey.Should().Be("map-test-key");
    }
}
