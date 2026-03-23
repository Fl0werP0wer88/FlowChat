using FlowChat.Messaging.Contracts.AuthService.Events;
using FlowChat.NotificationService.Consumers.NotificationApi.Contracts;
using FlowChat.NotificationService.Consumers.Services;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Subscribers;

namespace FlowChat.NotificationService.Consumers.Kafka;

public sealed class UserEmailVerificationRequestedSubscriber(
    INotificationInternalApiClient notificationInternalApiClient,
    ILogger<UserEmailVerificationRequestedSubscriber> logger)
{
    private readonly INotificationInternalApiClient _notificationInternalApiClient = notificationInternalApiClient
        ?? throw new ArgumentNullException(nameof(notificationInternalApiClient));
    private readonly ILogger<UserEmailVerificationRequestedSubscriber> _logger = logger
        ?? throw new ArgumentNullException(nameof(logger));

    [Subscribe]
    public async Task HandleAsync(
        EmailVerificationRequestIntegrationEvent message,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message.UserEmail))
        {
            throw new InvalidOperationException("Payload does not contain UserEmail.");
        }

        var userName = ResolveUserName(message.UserEmail);
        if (string.IsNullOrWhiteSpace(userName))
        {
            throw new InvalidOperationException("Payload does not contain valid UserEmail local-part.");
        }

        if (message.UserId == Guid.Empty)
        {
            throw new InvalidOperationException("Payload does not contain valid UserId.");
        }

        if (string.IsNullOrWhiteSpace(message.ConfirmationLink))
        {
            throw new InvalidOperationException("Payload does not contain ConfirmationLink.");
        }

        try
        {
            await _notificationInternalApiClient.ProcessUserEmailVerificationRequestedAsync(
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
        catch (InvalidOperationException ex)
        {
            _logger.LogInformation(
                ex,
                "Skipping notification handling for user {UserId}. Reason: {Reason}",
                message.UserId,
                ex.Message);

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
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
