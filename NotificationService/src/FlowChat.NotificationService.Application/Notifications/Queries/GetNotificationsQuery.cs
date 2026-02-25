using MediatR;

namespace FlowChat.NotificationService.Application.Notifications.Queries;

public sealed record GetNotificationsQuery(Guid? UserId) : IRequest<IReadOnlyList<NotificationDto>>;
