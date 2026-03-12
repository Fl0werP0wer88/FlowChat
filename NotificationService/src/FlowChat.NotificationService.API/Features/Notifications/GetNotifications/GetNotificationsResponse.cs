using FlowChat.NotificationService.Application.Notifications.Queries.GetNotifications;

namespace FlowChat.NotificationService.Api.Features.Notifications.GetNotifications;

public sealed record GetNotificationsResponse(IReadOnlyList<NotificationDto> Notifications);
