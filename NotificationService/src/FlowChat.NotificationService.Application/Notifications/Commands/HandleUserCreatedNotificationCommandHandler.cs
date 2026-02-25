using FlowChat.NotificationService.Application.Contracts.Infrastructure;
using FlowChat.NotificationService.Application.Contracts.Persistence;
using FlowChat.NotificationService.Domain.Entities;
using FlowChat.NotificationService.Domain.Enums;
using MediatR;

namespace FlowChat.NotificationService.Application.Notifications.Commands;

public sealed class HandleUserCreatedNotificationCommandHandler : IRequestHandler<HandleUserCreatedNotificationCommand>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationSender _notificationSender;

    public HandleUserCreatedNotificationCommandHandler(
        INotificationRepository notificationRepository,
        INotificationSender notificationSender)
    {
        _notificationRepository = notificationRepository;
        _notificationSender = notificationSender;
    }

    public async Task Handle(HandleUserCreatedNotificationCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty)
        {
            throw new InvalidOperationException("UserId is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new InvalidOperationException("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(request.UserName))
        {
            throw new InvalidOperationException("UserName is required.");
        }

        var displayName = string.IsNullOrWhiteSpace(request.DisplayName)
            ? request.UserName.Trim()
            : request.DisplayName.Trim();

        var alreadyExists = await _notificationRepository.ExistsByUserIdAndTypeAsync(
            request.UserId,
            NotificationType.Welcome,
            cancellationToken);

        if (alreadyExists)
        {
            return;
        }

        var notification = Notification.CreateWelcome(
            request.UserId,
            request.Email,
            displayName,
            request.SourceMessageKey);

        var sendRequest = new NotificationSendRequest(
            request.UserId,
            request.Email,
            "Welcome to FlowChat",
            $"Hello {displayName}, welcome to FlowChat.");

        var sendResult = await _notificationSender.SendAsync(sendRequest, cancellationToken);

        if (sendResult.IsSuccess)
        {
            notification.MarkSent(sendResult.ProviderMessageId);
        }
        else
        {
            notification.MarkFailed(sendResult.Error);
        }

        await _notificationRepository.AddAsync(notification, cancellationToken);
    }
}
