using MediatR;

namespace FlowChat.NotificationService.Application.Notifications.Queries.GetNotifications;

public sealed record GetNotificationsQuery(Guid? UserId) : IRequest<IReadOnlyList<NotificationDto>>;
