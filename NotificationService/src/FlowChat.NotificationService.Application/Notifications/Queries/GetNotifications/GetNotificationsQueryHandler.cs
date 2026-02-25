using FlowChat.NotificationService.Application.Contracts.Persistence;
using MediatR;

namespace FlowChat.NotificationService.Application.Notifications.Queries.GetNotifications;

public sealed class GetNotificationsQueryHandler : IRequestHandler<GetNotificationsQuery, IReadOnlyList<NotificationDto>>
{
    private readonly INotificationRepository _notificationRepository;

    public GetNotificationsQueryHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<IReadOnlyList<NotificationDto>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        var entities = request.UserId.HasValue
            ? await _notificationRepository.GetByUserIdAsync(request.UserId.Value, cancellationToken)
            : await _notificationRepository.GetRecentAsync(cancellationToken);

        return entities
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
    }
}
