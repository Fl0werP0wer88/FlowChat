using FlowChat.Shared.Application;
using FlowChat.NotificationService.Application.Contracts.Infrastructure;
using FlowChat.NotificationService.Application.Contracts.Persistence;
using FlowChat.NotificationService.Domain.Enums;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using MediatR;
using NotificationEntity = FlowChat.NotificationService.Domain.Entities.Notification.Notification;

namespace FlowChat.NotificationService.Application.Features.Notification.Commands.UserEmailVerificationRequested;

public sealed class UserEmailVerificationRequestedCommandHandler
    : CommandHandlerBase<UserEmailVerificationRequestedCommand, Unit>
{
    private readonly INotificationReadRepository _notificationReadRepository;
    private readonly INotificationWriteRepository _notificationWriteRepository;
    private readonly INotificationSender _notificationSender;
    private NotificationEntity? _notification;

    public UserEmailVerificationRequestedCommandHandler(
        INotificationReadRepository notificationReadRepository,
        INotificationWriteRepository notificationWriteRepository,
        INotificationSender notificationSender,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher) : base(domainEventDispatcher, unitOfWork)
    {
        _notificationReadRepository = notificationReadRepository;
        _notificationWriteRepository = notificationWriteRepository;
        _notificationSender = notificationSender;
    }

    protected override async Task<FlowChatResult<Unit>> ExecuteAsync(
        UserEmailVerificationRequestedCommand request,
        CancellationToken cancellationToken)
    {
        var displayName = string.IsNullOrWhiteSpace(request.DisplayName)
            ? request.UserName.Trim()
            : request.DisplayName.Trim();
        var emailAddress = EmailAddress.Create(request.Email);
        var notificationBody = $"Hello {displayName}, please confirm your email by clicking the link: {request.ConfirmationLink.Trim()}";

        // Idempotency guard: Kafka may redeliver the same message. If a notification was already
        // sent for this source message key, succeed without resending to avoid duplicate emails.
        var alreadyExists = await _notificationReadRepository.ExistsBySourceMessageKeyAsync(
            request.SourceMessageKey ?? string.Empty,
            cancellationToken);

        if (alreadyExists)
        {
            return FlowChatResult<Unit>.Success(Unit.Value);
        }

        _notification = NotificationEntity.CreateEmailVerification(
            Id<NotificationEntity>.New(),
            request.UserId,
            emailAddress,
            displayName,
            notificationBody,
            request.SourceMessageKey);

        var sendRequest = new NotificationSendRequest(
            request.UserId,
            emailAddress.Value,
            "Confirm your email in FlowChat",
            notificationBody);

        var sendResult = await _notificationSender.SendAsync(sendRequest, cancellationToken);

        if (!sendResult.IsSuccess)
        {
            return FlowChatResult<Unit>.Failure(
                DomainError.UnExpected(
                    $"Email delivery failed for user '{request.UserId}': {sendResult.Error ?? "unknown error"}"));
        }

        _notification.MarkSent(sendResult.ProviderMessageId);
        await _notificationWriteRepository.AddAsync(_notification, cancellationToken);

        return FlowChatResult<Unit>.Success(Unit.Value);
    }

    protected override IAggregateRoot? GetAggregateRoot(FlowChatResult<Unit> result)
    {
        return result.IsSuccess ? _notification : null;
    }
}

