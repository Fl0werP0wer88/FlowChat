using CSharpFunctionalExtensions;
using FlowChat.Application.Abstractions;
using FlowChat.Domain.Abstractions;
using FlowChat.NotificationService.Application.Contracts.Persistence;

namespace FlowChat.NotificationService.Application.Notifications.Queries.GetNotifications;

public sealed class GetNotificationsQueryHandler : IQueryHandler<GetNotificationsQuery, IReadOnlyList<NotificationDto>>
{
    private readonly INotificationReadRepository _notificationRepository;

    public GetNotificationsQueryHandler(INotificationReadRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<Result<IReadOnlyList<NotificationDto>, IDomainError>> Handle(
        GetNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var notifications = request.UserId.HasValue
            ? await _notificationRepository.GetByUserIdAsync(request.UserId.Value, cancellationToken)
            : await _notificationRepository.GetRecentAsync(cancellationToken);

        return Result.Success<IReadOnlyList<NotificationDto>, IDomainError>(notifications);
    }
}
