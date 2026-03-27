using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.NotificationService.Application.Contracts.Infrastructure;
using FlowChat.NotificationService.Application.Contracts.Persistence;
using FlowChat.NotificationService.Application.Features.Notifications.Commands.UserEmailVerificationRequested;
using FlowChat.NotificationService.Application.Features.Notifications.Queries.GetNotifications;
using FlowChat.NotificationService.Domain.Entities;
using FlowChat.NotificationService.Domain.Enums;
using MediatR;

namespace FlowChat.NotificationService.UnitTests;

public class HandleUserEmailVerificationRequestedNotificationCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_CreateSentNotification_WhenSenderReturnsSuccess()
    {
        var repository = new InMemoryNotificationRepository();
        var sender = new StubNotificationSender(
            new NotificationSendResult(true, "provider-123", null));
        var sut = new UserEmailVerificationRequestedCommandHandler(
            repository,
            repository,
            sender,
            new TestUnitOfWork(),
            new TestDomainEventDispatcher());

        var command = new UserEmailVerificationRequestedCommand(
            Guid.NewGuid(),
            "john@flowchat.local",
            "john",
            "John",
            "https://flowchat.local/confirm?userId=1&token=abc",
            "message-key-1");

        var result = await sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(repository.Notifications);
        var saved = repository.Notifications.Single();
        Assert.Equal(NotificationStatus.Sent, saved.Status);
        Assert.Equal(NotificationType.Welcome, saved.Type);
        Assert.Equal("provider-123", saved.ProviderMessageId);
        Assert.Null(saved.FailureReason);
        Assert.NotNull(sender.LastRequest);
        Assert.Contains(command.ConfirmationLink, sender.LastRequest!.Body);
    }

    [Fact]
    public async Task Handle_Should_NotCreateDuplicate_WhenWelcomeNotificationAlreadyExists()
    {
        var repository = new InMemoryNotificationRepository();
        var existing = Notification.CreateWelcome(
            Guid.NewGuid(),
            "existing@flowchat.local",
            "Existing",
            "source-key");
        existing.MarkSent("provider-existing");
        repository.Notifications.Add(existing);

        var sender = new StubNotificationSender(
            new NotificationSendResult(true, "provider-new", null));
        var sut = new UserEmailVerificationRequestedCommandHandler(
            repository,
            repository,
            sender,
            new TestUnitOfWork(),
            new TestDomainEventDispatcher());

        var command = new UserEmailVerificationRequestedCommand(
            existing.UserId,
            "existing@flowchat.local",
            "existing-user",
            "Existing",
            "https://flowchat.local/confirm?userId=2&token=def",
            "message-key-2");

        var result = await sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(repository.Notifications);
        Assert.Equal(0, sender.CallsCount);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailureAndNotPersist_WhenSenderReturnsFailureResult()
    {
        var repository = new InMemoryNotificationRepository();
        var sender = new StubNotificationSender(
            new NotificationSendResult(false, null, "smtp timeout"));
        var sut = new UserEmailVerificationRequestedCommandHandler(
            repository,
            repository,
            sender,
            new TestUnitOfWork(),
            new TestDomainEventDispatcher());

        var command = new UserEmailVerificationRequestedCommand(
            Guid.NewGuid(),
            "retry@flowchat.local",
            "retry",
            "Retry",
            "https://flowchat.local/confirm?userId=4&token=jkl",
            "message-key-4");

        var result = await sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unexpected, result.Error.ErrorType);
        Assert.Empty(repository.Notifications);
        Assert.Equal(1, sender.CallsCount);
    }

    [Fact]
    public async Task Handle_Should_ReturnValidationFailure_WhenUserIdIsEmpty()
    {
        var repository = new InMemoryNotificationRepository();
        var sender = new StubNotificationSender(
            new NotificationSendResult(true, "provider-123", null));
        var sut = new UserEmailVerificationRequestedCommandHandler(
            repository,
            repository,
            sender,
            new TestUnitOfWork(),
            new TestDomainEventDispatcher());

        var command = new UserEmailVerificationRequestedCommand(
            Guid.Empty,
            "john@flowchat.local",
            "john",
            "John",
            "https://flowchat.local/confirm?userId=3&token=ghi",
            "message-key-3");

        var result = await sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
        Assert.Empty(repository.Notifications);
    }

    private sealed class StubNotificationSender : INotificationSender
    {
        private readonly NotificationSendResult _result;

        public StubNotificationSender(NotificationSendResult result)
        {
            _result = result;
        }

        public int CallsCount { get; private set; }
        public NotificationSendRequest? LastRequest { get; private set; }

        public Task<NotificationSendResult> SendAsync(
            NotificationSendRequest request,
            CancellationToken cancellationToken = default)
        {
            CallsCount++;
            LastRequest = request;
            return Task.FromResult(_result);
        }
    }

    private sealed class InMemoryNotificationRepository : INotificationReadRepository, INotificationWriteRepository
    {
        public List<Notification> Notifications { get; } = [];

        Task<Notification?> FlowChat.Shared.Application.IWriteRepository<Notification>.GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken) =>
            Task.FromResult(Notifications.FirstOrDefault(x => x.Id.Value == id));

        public Task<Notification> AddAsync(Notification entity, CancellationToken cancellationToken = default)
        {
            Notifications.Add(entity);
            return Task.FromResult(entity);
        }

        public Task UpdateAsync(Notification entity, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Notification entity, CancellationToken cancellationToken = default)
        {
            Notifications.Remove(entity);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsByUserIdAndTypeAsync(
            Guid userId,
            NotificationType type,
            CancellationToken cancellationToken = default)
        {
            var exists = Notifications.Any(x => x.UserId == userId && x.Type == type);
            return Task.FromResult(exists);
        }

        public Task<NotificationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Notifications
                .Where(x => x.Id.Value == id)
                .Select(ToDto)
                .FirstOrDefault());
        }

        public Task<IReadOnlyList<NotificationDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult((IReadOnlyList<NotificationDto>)Notifications.Select(ToDto).ToArray());
        }

        public Task<IReadOnlyList<NotificationDto>> GetByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult((IReadOnlyList<NotificationDto>)Notifications
                .Where(x => x.UserId == userId)
                .Select(ToDto)
                .ToArray());
        }

        public Task<IReadOnlyList<NotificationDto>> GetRecentAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult((IReadOnlyList<NotificationDto>)Notifications.Select(ToDto).ToArray());
        }

        private static NotificationDto ToDto(Notification notification) => new(
            notification.Id.Value,
            notification.UserId,
            notification.Email,
            notification.DisplayName,
            notification.Type,
            notification.Status,
            notification.ProviderMessageId,
            notification.FailureReason,
            notification.SourceMessageKey,
            notification.SentAtUtc,
            notification.CreatedAtUtc.UtcDateTime);
    }

    private sealed class TestUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);

        public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
            => operation(cancellationToken);

        public void Dispose()
        {
        }
    }

    private sealed class TestDomainEventDispatcher : IDomainEventDispatcher
    {
        public Task DispatchAsync(IEnumerable<IDomainEvent> initialEvents, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}

