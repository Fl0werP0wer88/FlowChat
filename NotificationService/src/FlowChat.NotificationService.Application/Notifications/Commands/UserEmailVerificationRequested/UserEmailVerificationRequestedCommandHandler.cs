using System.Data.SqlTypes;
using FlowChat.NotificationService.Application.Contracts.Infrastructure;
using FlowChat.NotificationService.Application.Contracts.Persistence;
using FlowChat.NotificationService.Domain.Entities;
using FlowChat.NotificationService.Domain.Enums;
using MediatR;

namespace FlowChat.NotificationService.Application.Notifications.Commands.UserEmailVerificationRequested;

public sealed class UserEmailVerificationRequestedCommandHandler : IRequestHandler<UserEmailVerificationRequestedCommand>
{
    private readonly INotificationReadRepository _notificationReadRepository;
    private readonly INotificationWriteRepository _notificationWriteRepository;
    private readonly INotificationSender _notificationSender;

    public UserEmailVerificationRequestedCommandHandler(
        INotificationReadRepository notificationReadRepository,
        INotificationWriteRepository notificationWriteRepository,
        INotificationSender notificationSender)
    {
        _notificationReadRepository = notificationReadRepository;
        _notificationWriteRepository = notificationWriteRepository;
        _notificationSender = notificationSender;
    }

    public async Task Handle(UserEmailVerificationRequestedCommand request, CancellationToken cancellationToken)
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

        if (string.IsNullOrWhiteSpace(request.ConfirmationLink))
        {
            throw new InvalidOperationException("ConfirmationLink is required.");
        }

        var displayName = string.IsNullOrWhiteSpace(request.DisplayName)
            ? request.UserName.Trim()
            : request.DisplayName.Trim();

        var alreadyExists = await _notificationReadRepository.ExistsByUserIdAndTypeAsync(
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
            "Confirm your email in FlowChat",
            $"Hello {displayName}, please confirm your email by clicking the link: {request.ConfirmationLink.Trim()}");

        var sendResult = await _notificationSender.SendAsync(sendRequest, cancellationToken);

        if (!sendResult.IsSuccess)
        {
            throw new Exception(
                $"Email delivery failed for user '{request.UserId}': {sendResult.Error ?? "unknown error"}");
        }

        notification.MarkSent(sendResult.ProviderMessageId);
        await _notificationWriteRepository.AddAsync(notification, cancellationToken);
    }
}
