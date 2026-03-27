using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.NotificationService.Consumers.NotificationApi.Contracts;
using FlowChat.NotificationService.Consumers.Services;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Subscribers;

namespace FlowChat.NotificationService.Consumers.Kafka;

public sealed class UserEmailVerificationRequestedSubscriber(
    INotificationInternalApiClient notificationInternalApiClient,
    ILogger<UserEmailVerificationRequestedSubscriber> logger)
{
    [Subscribe]
    public async Task HandleAsync(
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

        try
        {
            await notificationInternalApiClient.ProcessUserEmailVerificationRequestedAsync(
                new ProcessUserEmailVerificationRequestedRequest
                {
                    UserId = message.UserId,
                    Email = message.UserEmail.Trim(),
                    UserName = userName,
                    DisplayName = userName,
                    ConfirmationLink = message.ConfirmationLink.Trim(),
                    SourceMessageKey = message.UserId.ToString()
                },
                cancellationToken);
        }
        catch (NonTransientException ex)
        {
            logger.LogInformation(
                ex,
                "Skipping notification handling for user {UserId}. Reason: {Reason}",
                message.UserId,
                ex.Message);

            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Transient failure while handling notification for user {UserId}.",
                message.UserId);

            throw;
        }
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
