using AutoFixture;
using FlowChat.NotificationService.Domain.Entities.Notification;
using FlowChat.NotificationService.Domain.Enums;
using FlowChat.NotificationService.Persistence;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.Shared.Persistance.Auditing;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.NotificationService.IntegrationTests.Persistence.Configuration.Entities;

public sealed class NotificationConfigurationTests : IDisposable
{
    private readonly IFixture _fixture = new Fixture();
    private readonly AppDbContext _dbContext;

    public NotificationConfigurationTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(_fixture.Create<Guid>().ToString("N"))
            .AddInterceptors(new EntityBaseSaveChangesInterceptor())
            .Options;

        _dbContext = new AppDbContext(options);
    }

    public void Dispose() => _dbContext.Dispose();

    [Fact]
    public async Task NotificationConfiguration_CanPersistAndReloadAllProperties()
    {
        var userId = _fixture.Create<Guid>();
        var notification = Notification.CreateEmailVerification(
            Id<Notification>.New(),
            userId,
            EmailAddress.Create("config-test@example.com"),
            "Config Test User",
            "Confirm your email by clicking the provided link",
            "config-source-key");
        notification.MarkSent("provider-msg-xyz");

        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync();

        _dbContext.ChangeTracker.Clear();

        var reloaded = await _dbContext.Notifications.FindAsync(notification.Id);

        reloaded.Should().NotBeNull();
        reloaded!.UserId.Should().Be(userId);
        reloaded.Email.Value.Should().Be("config-test@example.com");
        reloaded.DisplayName.Should().Be("Config Test User");
        reloaded.Body.Should().Be("Confirm your email by clicking the provided link");
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
            Id<Notification>.New(),
            _fixture.Create<Guid>(),
            EmailAddress.Create("fail@example.com"),
            "Fail User",
            "Delivery failed for this content",
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
            Id<Notification>.New(), _fixture.Create<Guid>(), EmailAddress.Create("id-test@example.com"), "ID Test", "ID body", null);

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
            Id<Notification>.New(), _fixture.Create<Guid>(), EmailAddress.Create("welcome@example.com"), "Welcome User", "Welcome to FlowChat", "welcome-key");

        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync();

        _dbContext.ChangeTracker.Clear();

        var reloaded = await _dbContext.Notifications.FindAsync(notification.Id);

        reloaded.Should().NotBeNull();
        reloaded!.Body.Should().Be("Welcome to FlowChat");
        reloaded.Type.Should().Be(NotificationType.Welcome);
        reloaded.Status.Should().Be(NotificationStatus.Pending);
    }
}
