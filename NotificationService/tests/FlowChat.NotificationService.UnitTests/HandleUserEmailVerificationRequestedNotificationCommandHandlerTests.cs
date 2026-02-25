using FlowChat.NotificationService.Application.Contracts.Infrastructure;
using FlowChat.NotificationService.Application.Contracts.Persistence;
using FlowChat.NotificationService.Application.Notifications.Commands.UserEmailVerificationRequested;
using FlowChat.NotificationService.Domain.Entities;
using FlowChat.NotificationService.Domain.Enums;

namespace FlowChat.NotificationService.UnitTests;

public class HandleUserEmailVerificationRequestedNotificationCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_CreateSentNotification_WhenSenderReturnsSuccess()
    {
        var repository = new InMemoryNotificationRepository();
        var sender = new StubNotificationSender(
            new NotificationSendResult(true, "provider-123", null));
        var sut = new UserEmailVerificationRequestedCommandHandler(repository, sender);

        var command = new UserEmailVerificationRequestedCommand(
            Guid.NewGuid(),
            "john@flowchat.local",
            "john",
            "John",
            "https://flowchat.local/confirm?userId=1&token=abc",
            "message-key-1");

        await sut.Handle(command, CancellationToken.None);

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
        var sut = new UserEmailVerificationRequestedCommandHandler(repository, sender);

        var command = new UserEmailVerificationRequestedCommand(
            existing.UserId,
            "existing@flowchat.local",
            "existing-user",
            "Existing",
            "https://flowchat.local/confirm?userId=2&token=def",
            "message-key-2");

        await sut.Handle(command, CancellationToken.None);

        Assert.Single(repository.Notifications);
        Assert.Equal(0, sender.CallsCount);
    }

    [Fact]
    public async Task Handle_Should_ThrowAndNotPersist_WhenSenderReturnsFailureResult()
    {
        var repository = new InMemoryNotificationRepository();
        var sender = new StubNotificationSender(
            new NotificationSendResult(false, null, "smtp timeout"));
        var sut = new UserEmailVerificationRequestedCommandHandler(repository, sender);

        var command = new UserEmailVerificationRequestedCommand(
            Guid.NewGuid(),
            "retry@flowchat.local",
            "retry",
            "Retry",
            "https://flowchat.local/confirm?userId=4&token=jkl",
            "message-key-4");

        await Assert.ThrowsAsync<Exception>(() => sut.Handle(command, CancellationToken.None));

        Assert.Empty(repository.Notifications);
        Assert.Equal(1, sender.CallsCount);
    }

    [Fact]
    public async Task Handle_Should_Throw_WhenUserIdIsEmpty()
    {
        var repository = new InMemoryNotificationRepository();
        var sender = new StubNotificationSender(
            new NotificationSendResult(true, "provider-123", null));
        var sut = new UserEmailVerificationRequestedCommandHandler(repository, sender);

        var command = new UserEmailVerificationRequestedCommand(
            Guid.Empty,
            "john@flowchat.local",
            "john",
            "John",
            "https://flowchat.local/confirm?userId=3&token=ghi",
            "message-key-3");

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.Handle(command, CancellationToken.None));
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

    private sealed class InMemoryNotificationRepository : INotificationRepository
    {
        public List<Notification> Notifications { get; } = [];

        public Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Notifications.FirstOrDefault(x => x.Id == id));
        }

        public Task<IReadOnlyList<Notification>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult((IReadOnlyList<Notification>)Notifications.ToArray());
        }

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

        public Task<IReadOnlyList<Notification>> GetByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult((IReadOnlyList<Notification>)Notifications
                .Where(x => x.UserId == userId)
                .ToArray());
        }

        public Task<IReadOnlyList<Notification>> GetRecentAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult((IReadOnlyList<Notification>)Notifications.ToArray());
        }
    }
}
