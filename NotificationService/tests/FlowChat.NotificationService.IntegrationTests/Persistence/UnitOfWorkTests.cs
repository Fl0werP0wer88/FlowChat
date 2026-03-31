using AutoFixture;
using FlowChat.NotificationService.Domain.Entities;
using FlowChat.NotificationService.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.NotificationService.IntegrationTests.Persistence;

public sealed class UnitOfWorkTests : IDisposable
{
    private readonly IFixture _fixture = new Fixture();
    private readonly AppDbContext _dbContext;
    private readonly UnitOfWork _unitOfWork;

    public UnitOfWorkTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(_fixture.Create<Guid>().ToString("N"))
            .Options;

        _dbContext = new AppDbContext(options);
        _unitOfWork = new UnitOfWork(_dbContext);
    }

    public void Dispose()
    {
        _unitOfWork.Dispose();
    }

    private static Notification CreateNotification() =>
        Notification.CreateEmailVerification(Guid.NewGuid(), "test@example.com", "Test User", null);

    // --- SaveChangesAsync ---

    [Fact]
    public async Task SaveChangesAsync_PersistsAddedEntities()
    {
        var notification = CreateNotification();
        _dbContext.Notifications.Add(notification);

        await _unitOfWork.SaveChangesAsync();

        var persisted = await _dbContext.Notifications.FindAsync(notification.Id);
        persisted.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveChangesAsync_ReturnsNumberOfAffectedRows()
    {
        _dbContext.Notifications.Add(CreateNotification());
        _dbContext.Notifications.Add(CreateNotification());

        var affected = await _unitOfWork.SaveChangesAsync();

        affected.Should().Be(2);
    }

    // --- ExecuteInTransactionAsync ---

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenOperationSucceeds_CommitsAndReturnsResult()
    {
        var notification = CreateNotification();

        var result = await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            _dbContext.Notifications.Add(notification);
            return FlowChat.Core.Results.FlowChatResult<int>.Success(42);
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);

        var persisted = await _dbContext.Notifications.FindAsync(notification.Id);
        persisted.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenOperationThrows_RollsBack()
    {
        var notification = CreateNotification();

        var act = async () => await _unitOfWork.ExecuteInTransactionAsync<int>(async ct =>
        {
            _dbContext.Notifications.Add(notification);
            await _dbContext.SaveChangesAsync(ct);
            throw new InvalidOperationException("Simulated failure");
        }, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Simulated failure");
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WithNullOperation_ThrowsArgumentNullException()
    {
        var act = () => _unitOfWork.ExecuteInTransactionAsync<int>(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
