using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.NotificationService.Consumers.NotificationApi.Contracts;
using FlowChat.NotificationService.Consumers.Services;
using FlowChat.Shared.Infrastructure.Silverback.Subscribers;

namespace FlowChat.NotificationService.Consumers.Kafka;

public sealed class UserEmailVerificationRequestedSubscriber(
    INotificationInternalApiClient notificationInternalApiClient,
    ILogger<UserEmailVerificationRequestedSubscriber> logger)
    : SubscriberBase<EmailVerificationRequestIntegrationEvent>(logger)
{
    protected override async Task ExecuteAsync(
        EmailVerificationRequestIntegrationEvent message,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message.UserEmail))
        {
            throw new NonTransientException("Payload does not contain UserEmail.");
        }

        var userName = ResolveUserName(message.UserEmail);
        if (string.IsNullOrWhiteSpace(userName))
        {
            throw new NonTransientException("Payload does not contain valid UserEmail local-part.");
        }

        if (message.UserId == Guid.Empty)
        {
            throw new NonTransientException("Payload does not contain valid UserId.");
        }

        if (string.IsNullOrWhiteSpace(message.ConfirmationLink))
        {
            throw new NonTransientException("Payload does not contain ConfirmationLink.");
        }

        await notificationInternalApiClient.ProcessUserEmailVerificationRequestedAsync(
            new ProcessUserEmailVerificationRequestedRequest
            {
                UserId = message.UserId,
                Email = message.UserEmail.Trim(),
                UserName = userName,
                DisplayName = userName,
                ConfirmationLink = message.ConfirmationLink.Trim(),
                SourceMessageKey = string.IsNullOrWhiteSpace(message.Key)
                    ? message.UserId.ToString()
                    : message.Key.Trim()
            },
            cancellationToken);
    }

    private static string? ResolveUserName(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var value = email.Trim();
        var atIndex = value.IndexOf('@');
        if (atIndex <= 0)
        {
            return null;
        }

        return value[..atIndex].Trim();
    }
}
