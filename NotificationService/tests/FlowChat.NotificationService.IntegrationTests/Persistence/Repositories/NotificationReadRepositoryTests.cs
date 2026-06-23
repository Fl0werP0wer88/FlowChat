using AutoFixture;
using FlowChat.NotificationService.Domain.Entities.Notification;
using FlowChat.NotificationService.Domain.Enums;
using FlowChat.NotificationService.Persistence;
using FlowChat.NotificationService.Persistence.Repositories;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.Shared.Persistance.Auditing;
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
            .AddInterceptors(new EntityBaseSaveChangesInterceptor())
            .Options;

        _dbContext = new AppDbContext(options);
        _repository = new NotificationReadRepository(_dbContext);
    }

    public void Dispose() => _dbContext.Dispose();

    private async Task<Notification> SeedNotificationAsync(
        Guid? userId = null,
        NotificationType type = NotificationType.EmailVerification,
        string? sourceMessageKey = null)
    {
        var notification = Notification.CreateEmailVerification(
            Id<Notification>.New(),
            userId ?? _fixture.Create<Guid>(),
            EmailAddress.Create($"{_fixture.Create<string>()}@example.com"),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            sourceMessageKey);

        _dbContext.Notifications.Add(notification);
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
    public async Task GetRecentAsync_WhenNoNotifications_ReturnsEmptyList()
    {
        var result = await _repository.GetRecentAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRecentAsync_MapsAllExpectedFields()
    {
        var userId = _fixture.Create<Guid>();
        var notification = Notification.CreateEmailVerification(
            Id<Notification>.New(),
            userId,
            EmailAddress.Create("test@example.com"),
            "Test User",
            "Confirm your email by clicking the provided link",
            "map-test-key");
        notification.MarkSent("provider-msg-123");

        _dbContext.Notifications.Add(notification);
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
