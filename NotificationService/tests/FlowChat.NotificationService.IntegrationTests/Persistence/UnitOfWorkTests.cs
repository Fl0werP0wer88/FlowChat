using AutoFixture;
using FlowChat.NotificationService.Domain.Entities.Notification;
using FlowChat.NotificationService.Persistence;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.Shared.Persistance;
using FlowChat.Shared.Persistance.Auditing;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.NotificationService.IntegrationTests.Persistence;

public sealed class UnitOfWorkTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _dbContext;
    private readonly EfUnitOfWork<AppDbContext> _unitOfWork;

    public UnitOfWorkTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(new EntityBaseSaveChangesInterceptor())
            .Options;

        _dbContext = new AppDbContext(options);
        _dbContext.Database.EnsureCreated();
        _unitOfWork = new EfUnitOfWork<AppDbContext>(_dbContext);
    }

    public void Dispose()
    {
        _unitOfWork.Dispose();
        _connection.Dispose();
    }

    private static Notification CreateNotification() =>
        Notification.CreateEmailVerification(
            Id<Notification>.New(),
            Guid.NewGuid(),
            EmailAddress.Create("test@example.com"),
            "Test User",
            "Confirm your email by clicking the provided link",
            null);

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
