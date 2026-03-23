using FlowChat.NotificationService.Application.Notifications.Queries.GetNotifications;

namespace FlowChat.NotificationService.Api.Features.Notifications.Public.GetNotifications;

public sealed record GetNotificationsResponse(IReadOnlyList<NotificationDto> Notifications);
