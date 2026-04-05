using AutoFixture;
using FlowChat.NotificationService.Domain.Entities.Notification;
using FlowChat.NotificationService.Domain.Enums;
using FlowChat.NotificationService.Persistence;
using FlowChat.NotificationService.Persistence.Repositories;
using FlowChat.Shared.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.NotificationService.IntegrationTests.Persistence.Repositories;

public sealed class NotificationWriteRepositoryTests : IDisposable
{
    private readonly IFixture _fixture = new Fixture();
    private readonly AppDbContext _dbContext;
    private readonly NotificationWriteRepository _repository;

    public NotificationWriteRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(_fixture.Create<Guid>().ToString("N"))
            .Options;

        _dbContext = new AppDbContext(options);
        _repository = new NotificationWriteRepository(_dbContext);
    }

    public void Dispose() => _dbContext.Dispose();

    private static Notification CreateNotification(Guid? userId = null, string? sourceMessageKey = null)
    {
        return Notification.CreateEmailVerification(
            userId ?? Guid.NewGuid(),
            EmailAddress.Create("test@example.com"),
            "Test User",
            sourceMessageKey);
    }

    // --- AddAsync ---

    [Fact]
    public async Task AddAsync_WithValidEntity_PersistsToDatabase()
    {
        var notification = CreateNotification();

        await _repository.AddAsync(notification);
        await _dbContext.SaveChangesAsync();

        var persisted = await _dbContext.Notifications.FindAsync(notification.Id);
        persisted.Should().NotBeNull();
        persisted!.Email.Value.Should().Be("test@example.com");
    }

    [Fact]
    public async Task AddAsync_ReturnsAddedEntity()
    {
        var notification = CreateNotification();

        var result = await _repository.AddAsync(notification);

        result.Should().BeSameAs(notification);
    }

    // --- GetByIdAsync ---

    [Fact]
    public async Task GetByIdAsync_WhenEntityExists_ReturnsEntity()
    {
        var notification = CreateNotification();
        await _repository.AddAsync(notification);
        await _dbContext.SaveChangesAsync();

        var found = await _repository.GetByIdAsync(notification.Id.Value);

        found.Should().NotBeNull();
        found!.Id.Should().Be(notification.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenEntityDoesNotExist_ReturnsNull()
    {
        var found = await _repository.GetByIdAsync(Guid.NewGuid());

        found.Should().BeNull();
    }

    // --- UpdateAsync ---

    [Fact]
    public async Task UpdateAsync_WithChangedState_PersistsChanges()
    {
        var notification = CreateNotification();
        await _repository.AddAsync(notification);
        await _dbContext.SaveChangesAsync();

        notification.MarkSent("provider-id-abc");
        await _repository.UpdateAsync(notification);
        await _dbContext.SaveChangesAsync();

        var updated = await _dbContext.Notifications.FindAsync(notification.Id);
        updated!.Status.Should().Be(NotificationStatus.Sent);
        updated.ProviderMessageId.Should().Be("provider-id-abc");
    }

    // --- DeleteAsync ---

    [Fact]
    public async Task DeleteAsync_WithExistingEntity_RemovesFromDatabase()
    {
        var notification = CreateNotification();
        await _repository.AddAsync(notification);
        await _dbContext.SaveChangesAsync();

        await _repository.DeleteAsync(notification);
        await _dbContext.SaveChangesAsync();

        var deleted = await _dbContext.Notifications.FindAsync(notification.Id);
        deleted.Should().BeNull();
    }
}
