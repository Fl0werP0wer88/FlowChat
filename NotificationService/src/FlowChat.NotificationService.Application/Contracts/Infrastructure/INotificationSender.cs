namespace FlowChat.NotificationService.Application.Contracts.Infrastructure;

public interface INotificationSender
{
    Task<NotificationSendResult> SendAsync(
        NotificationSendRequest request,
        CancellationToken cancellationToken = default);
}
