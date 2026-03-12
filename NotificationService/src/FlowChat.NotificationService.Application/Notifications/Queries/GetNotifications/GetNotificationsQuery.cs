using FlowChat.Application.Abstractions;

namespace FlowChat.NotificationService.Application.Notifications.Queries.GetNotifications;

public sealed record GetNotificationsQuery(Guid? UserId) : IQuery<IReadOnlyList<NotificationDto>>;
