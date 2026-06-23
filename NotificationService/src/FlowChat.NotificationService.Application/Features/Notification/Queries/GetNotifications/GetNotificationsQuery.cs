using FlowChat.Shared.Application;

namespace FlowChat.NotificationService.Application.Features.Notification.Queries.GetNotifications;

public sealed record GetNotificationsQuery(Guid? UserId) : IQuery<IReadOnlyList<NotificationDto>>;

