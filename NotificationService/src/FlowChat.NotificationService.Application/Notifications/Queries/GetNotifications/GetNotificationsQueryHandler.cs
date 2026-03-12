using CSharpFunctionalExtensions;
using FlowChat.Application.Abstractions;
using FlowChat.Domain.Abstractions;
using FlowChat.NotificationService.Application.Contracts.Persistence;

namespace FlowChat.NotificationService.Application.Notifications.Queries.GetNotifications;

public sealed class GetNotificationsQueryHandler : IQueryHandler<GetNotificationsQuery, IReadOnlyList<NotificationDto>>
{
    private readonly INotificationRepository _notificationRepository;

    public GetNotificationsQueryHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<Result<IReadOnlyList<NotificationDto>, IDomainError>> Handle(
        GetNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var entities = request.UserId.HasValue
            ? await _notificationRepository.GetByUserIdAsync(request.UserId.Value, cancellationToken)
            : await _notificationRepository.GetRecentAsync(cancellationToken);

        var notifications = entities
            .Select(notification => new NotificationDto(
                notification.Id,
                notification.UserId,
                notification.Email,
                notification.DisplayName,
                notification.Type,
                notification.Status,
                notification.ProviderMessageId,
                notification.FailureReason,
                notification.SourceMessageKey,
                notification.SentAtUtc,
                notification.CreatedDate))
            .ToArray();

        return Result.Success<IReadOnlyList<NotificationDto>, IDomainError>(notifications);
    }
}
