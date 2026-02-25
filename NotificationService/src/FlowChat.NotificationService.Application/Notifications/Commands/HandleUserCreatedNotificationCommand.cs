using MediatR;

namespace FlowChat.NotificationService.Application.Notifications.Commands;

public sealed record HandleUserCreatedNotificationCommand(
    Guid UserId,
    string Email,
    string UserName,
    string DisplayName,
    string? SourceMessageKey) : IRequest;
