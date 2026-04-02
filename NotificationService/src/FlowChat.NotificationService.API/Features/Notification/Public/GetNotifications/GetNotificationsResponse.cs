using FlowChat.NotificationService.Application.Features.Notification.Queries.GetNotifications;

namespace FlowChat.NotificationService.Api.Features.Notification.Public.GetNotifications;

public sealed record GetNotificationsResponse(IReadOnlyList<NotificationDto> Notifications);
