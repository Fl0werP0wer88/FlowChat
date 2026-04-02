using AutoFixture;
using FlowChat.NotificationService.Domain.Entities.Notification;
using FlowChat.NotificationService.Domain.Enums;
using FlowChat.NotificationService.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.NotificationService.IntegrationTests.Persistence.Configurations;

public sealed class NotificationConfigurationTests : IDisposable
{
    private readonly IFixture _fixture = new Fixture();
    private readonly AppDbContext _dbContext;

    public NotificationConfigurationTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(_fixture.Create<Guid>().ToString("N"))
            .Options;

        _dbContext = new AppDbContext(options);
    }

    public void Dispose() => _dbContext.Dispose();

    [Fact]
    public async Task NotificationConfiguration_CanPersistAndReloadAllProperties()
    {
        var userId = _fixture.Create<Guid>();
        var notification = Notification.CreateEmailVerification(
            userId,
            "config-test@example.com",
            "Config Test User",
            "config-source-key");
        notification.MarkSent("provider-msg-xyz");

        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync();

        _dbContext.ChangeTracker.Clear();

        var reloaded = await _dbContext.Notifications.FindAsync(notification.Id);

        reloaded.Should().NotBeNull();
        reloaded!.UserId.Should().Be(userId);
        reloaded.Email.Should().Be("config-test@example.com");
        reloaded.DisplayName.Should().Be("Config Test User");
        reloaded.Type.Should().Be(NotificationType.EmailVerification);
        reloaded.Status.Should().Be(NotificationStatus.Sent);
        reloaded.ProviderMessageId.Should().Be("provider-msg-xyz");
        reloaded.SourceMessageKey.Should().Be("config-source-key");
        reloaded.FailureReason.Should().BeNull();
        reloaded.SentAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task NotificationConfiguration_CanPersistFailedNotification()
    {
        var notification = Notification.CreateEmailVerification(
            _fixture.Create<Guid>(),
            "fail@example.com",
            "Fail User",
            null);
        notification.MarkFailed("connection refused");

        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync();

        _dbContext.ChangeTracker.Clear();

        var reloaded = await _dbContext.Notifications.FindAsync(notification.Id);

        reloaded.Should().NotBeNull();
        reloaded!.Status.Should().Be(NotificationStatus.Failed);
        reloaded.FailureReason.Should().Be("connection refused");
        reloaded.ProviderMessageId.Should().BeNull();
        reloaded.SentAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task NotificationConfiguration_MapsIdUsingTypedId()
    {
        var notification = Notification.CreateEmailVerification(
            _fixture.Create<Guid>(), "id-test@example.com", "ID Test", null);

        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync();

        _dbContext.ChangeTracker.Clear();

        var reloaded = await _dbContext.Notifications.FindAsync(notification.Id);

        reloaded.Should().NotBeNull();
        reloaded!.Id.Should().Be(notification.Id);
    }

    [Fact]
    public void NotificationConfiguration_TableIsNamedNotifications()
    {
        var entityType = _dbContext.Model.FindEntityType(typeof(Notification));

        entityType.Should().NotBeNull();
        entityType!.GetTableName().Should().Be("Notifications");
    }

    [Fact]
    public async Task NotificationConfiguration_CanPersistWelcomeNotification()
    {
        var notification = Notification.CreateWelcome(
            _fixture.Create<Guid>(), "welcome@example.com", "Welcome User", "welcome-key");

        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync();

        _dbContext.ChangeTracker.Clear();

        var reloaded = await _dbContext.Notifications.FindAsync(notification.Id);

        reloaded.Should().NotBeNull();
        reloaded!.Type.Should().Be(NotificationType.Welcome);
        reloaded.Status.Should().Be(NotificationStatus.Pending);
    }
}
