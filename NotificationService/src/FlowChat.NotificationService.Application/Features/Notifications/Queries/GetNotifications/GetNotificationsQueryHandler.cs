using CSharpFunctionalExtensions;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.NotificationService.Application.Contracts.Persistence;

namespace FlowChat.NotificationService.Application.Features.Notifications.Queries.GetNotifications;

public sealed class GetNotificationsQueryHandler : IQueryHandler<GetNotificationsQuery, IReadOnlyList<NotificationDto>>
{
    private readonly INotificationReadRepository _notificationRepository;

    public GetNotificationsQueryHandler(INotificationReadRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<FlowChatResult<IReadOnlyList<NotificationDto>>> Handle(
        GetNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var notifications = request.UserId.HasValue
            ? await _notificationRepository.GetByUserIdAsync(request.UserId.Value, cancellationToken)
            : await _notificationRepository.GetRecentAsync(cancellationToken);

        return FlowChatResult<IReadOnlyList<NotificationDto>>.Success(notifications);
    }
}

