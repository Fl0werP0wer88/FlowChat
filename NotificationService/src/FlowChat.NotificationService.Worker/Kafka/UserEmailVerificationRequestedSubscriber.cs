using FlowChat.Messaging.Contracts.AuthService.Events;
using FlowChat.NotificationService.Application.Notifications.Commands.UserEmailVerificationRequested;
using MediatR;
using Silverback.Messaging.Subscribers;

namespace FlowChat.NotificationService.Worker.Kafka;

public sealed class UserEmailVerificationRequestedSubscriber(
    IMediator mediator,
    ILogger<UserEmailVerificationRequestedSubscriber> logger)
{
    [Subscribe]
    public Task HandleAsync(
        UserCreatedIntegrationEvent message,
        CancellationToken cancellationToken)
    {
        logger.LogDebug(
            "Ignoring {EventType} for user {UserId} in NotificationService.",
            nameof(UserCreatedIntegrationEvent),
            message.UserId);

        return Task.CompletedTask;
    }

    [Subscribe]
    public Task HandleAsync(
        UserConfirmedIntegrationEvent message,
        CancellationToken cancellationToken)
    {
        logger.LogDebug(
            "Ignoring {EventType} for user {UserId} in NotificationService.",
            nameof(UserConfirmedIntegrationEvent),
            message.UserId);

        return Task.CompletedTask;
    }

    [Subscribe]
    public async Task HandleAsync(
        UserEmailVerificationRequestedIntegrationEvent message,
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

        try
        {
            await mediator.Send(
                new UserEmailVerificationRequestedCommand(
                    message.UserId,
                    message.UserEmail.Trim(),
                    userName,
                    userName,
                    message.ConfirmationLink,
                    message.UserId.ToString()),
                cancellationToken);
        }
        catch (InvalidOperationException ex)
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
