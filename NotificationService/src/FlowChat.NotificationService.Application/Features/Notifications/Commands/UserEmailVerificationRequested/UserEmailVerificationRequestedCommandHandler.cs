using CSharpFunctionalExtensions;
using FlowChat.Application.Abstractions;
using FlowChat.NotificationService.Application.Contracts.Infrastructure;
using FlowChat.NotificationService.Application.Contracts.Persistence;
using FlowChat.NotificationService.Domain.Entities;
using FlowChat.NotificationService.Domain.Enums;
using FlowChat.Domain.Abstractions;
using MediatR;

namespace FlowChat.NotificationService.Application.Features.Notifications.Commands.UserEmailVerificationRequested;

public sealed class UserEmailVerificationRequestedCommandHandler
    : CommandHandlerBase<UserEmailVerificationRequestedCommand, Unit>
{
    private readonly INotificationReadRepository _notificationReadRepository;
    private readonly INotificationWriteRepository _notificationWriteRepository;
    private readonly INotificationSender _notificationSender;
    private Notification? _notification;

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
        var validationErrors = new ValidationErrorCollector()
            .AddIf(request.UserId == Guid.Empty, "UserId is required.")
            .AddIf(string.IsNullOrWhiteSpace(request.Email), "Email is required.")
            .AddIf(string.IsNullOrWhiteSpace(request.UserName), "UserName is required.")
            .AddIf(string.IsNullOrWhiteSpace(request.ConfirmationLink), "ConfirmationLink is required.");

        if (validationErrors.HasErrors)
        {
            return validationErrors.ToFailure<Unit>();
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
            return FlowChatResult<Unit>.Success(Unit.Value);
        }

        _notification = Notification.CreateWelcome(
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
