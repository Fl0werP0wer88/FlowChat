using FlowChat.Shared.Application;

namespace FlowChat.NotificationService.Application.Features.Notifications.Queries.GetNotifications;

public sealed record GetNotificationsQuery(Guid? UserId) : IQuery<IReadOnlyList<NotificationDto>>;

