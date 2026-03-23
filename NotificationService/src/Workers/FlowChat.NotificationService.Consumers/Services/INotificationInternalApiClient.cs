using FlowChat.NotificationService.Consumers.NotificationApi.Contracts;

namespace FlowChat.NotificationService.Consumers.Services;

public interface INotificationInternalApiClient
{
    Task ProcessUserEmailVerificationRequestedAsync(
        ProcessUserEmailVerificationRequestedRequest request,
        CancellationToken cancellationToken);
}
