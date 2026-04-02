using AutoFixture;
using FlowChat.NotificationService.Application.Contracts.Infrastructure;
using FlowChat.NotificationService.Application.Contracts.Persistence;
using FlowChat.NotificationService.Application.Features.Notifications.Commands.UserEmailVerificationRequested;
using FlowChat.NotificationService.Domain.Entities.Notification;
using FlowChat.NotificationService.Domain.Enums;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Moq;

namespace FlowChat.NotificationService.UnitTests;

public sealed class HandleUserEmailVerificationRequestedNotificationCommandHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<INotificationReadRepository> _readRepositoryMock = new();
    private readonly Mock<INotificationWriteRepository> _writeRepositoryMock = new();
    private readonly Mock<INotificationSender> _notificationSenderMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IDomainEventDispatcher> _domainEventDispatcherMock = new();
    private readonly UserEmailVerificationRequestedCommandHandler _handler;

    public HandleUserEmailVerificationRequestedNotificationCommandHandlerTests()
    {
        _readRepositoryMock
            .Setup(x => x.ExistsBySourceMessageKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _writeRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Notification notification, CancellationToken _) => notification);

        _unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<Unit>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<Unit>>>, CancellationToken>((operation, ct) => operation(ct));

        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new UserEmailVerificationRequestedCommandHandler(
            _readRepositoryMock.Object,
            _writeRepositoryMock.Object,
            _notificationSenderMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object);
    }

    private async Task<FlowChatResult<Unit>> SendAsync(UserEmailVerificationRequestedCommand command)
    {
        var validator = new UserEmailVerificationRequestedCommandValidator();
        var validationResult = await validator.ValidateAsync(command);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(error => error.ErrorMessage).ToList();
            return FlowChatResult<Unit>.Failure(DomainError.Validation(errors: errors));
        }

        return await _handler.Handle(command, CancellationToken.None);
    }

    [Fact]
    public async Task Handle_WhenSenderReturnsSuccess_CreatesSentNotification()
    {
        Notification? savedNotification = null;
        NotificationSendRequest? sendRequest = null;

        _notificationSenderMock
            .Setup(x => x.SendAsync(It.IsAny<NotificationSendRequest>(), It.IsAny<CancellationToken>()))
            .Callback<NotificationSendRequest, CancellationToken>((request, _) => sendRequest = request)
            .ReturnsAsync(new NotificationSendResult(true, "provider-123", null));

        _writeRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Callback<Notification, CancellationToken>((notification, _) => savedNotification = notification)
            .ReturnsAsync((Notification notification, CancellationToken _) => notification);

        var command = new UserEmailVerificationRequestedCommand(
            _fixture.Create<Guid>(),
            "john@flowchat.local",
            "john",
            "John",
            "https://flowchat.local/confirm?userId=1&token=abc",
            "message-key-1");

        var result = await SendAsync(command);

        result.IsSuccess.Should().BeTrue();
        savedNotification.Should().NotBeNull();
        savedNotification!.Status.Should().Be(NotificationStatus.Sent);
        savedNotification.Type.Should().Be(NotificationType.EmailVerification);
        savedNotification.ProviderMessageId.Should().Be("provider-123");
        savedNotification.FailureReason.Should().BeNull();
        sendRequest.Should().NotBeNull();
        sendRequest!.Body.Should().Contain(command.ConfirmationLink);
    }

    [Fact]
    public async Task Handle_WhenSourceMessageKeyAlreadyExists_DoesNotCreateDuplicate()
    {
        _readRepositoryMock
            .Setup(x => x.ExistsBySourceMessageKeyAsync("message-key-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new UserEmailVerificationRequestedCommand(
            _fixture.Create<Guid>(),
            "existing@flowchat.local",
            "existing-user",
            "Existing",
            "https://flowchat.local/confirm?userId=2&token=def",
            "message-key-2");

        var result = await SendAsync(command);

        result.IsSuccess.Should().BeTrue();
        _notificationSenderMock.Verify(
            x => x.SendAsync(It.IsAny<NotificationSendRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _writeRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSenderReturnsFailureResult_ReturnsFailureAndDoesNotPersist()
    {
        _notificationSenderMock
            .Setup(x => x.SendAsync(It.IsAny<NotificationSendRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotificationSendResult(false, null, "smtp timeout"));

        var command = new UserEmailVerificationRequestedCommand(
            _fixture.Create<Guid>(),
            "retry@flowchat.local",
            "retry",
            "Retry",
            "https://flowchat.local/confirm?userId=4&token=jkl",
            "message-key-4");

        var result = await SendAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Unexpected);
        _writeRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _notificationSenderMock.Verify(
            x => x.SendAsync(It.IsAny<NotificationSendRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserIdIsEmpty_ReturnsValidationFailure()
    {
        var command = new UserEmailVerificationRequestedCommand(
            Guid.Empty,
            "john@flowchat.local",
            "john",
            "John",
            "https://flowchat.local/confirm?userId=3&token=ghi",
            "message-key-3");

        var result = await SendAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Validation);
        result.Error.Errors.Should().Contain("UserId is required.");
        _writeRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
