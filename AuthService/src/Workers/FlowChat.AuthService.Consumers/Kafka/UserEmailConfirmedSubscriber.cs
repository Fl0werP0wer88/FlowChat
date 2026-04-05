using FlowChat.AuthService.Consumers.AuthApi.Contracts;
using FlowChat.AuthService.Consumers.Services;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging.UserProfileService.Events;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Subscribers;

namespace FlowChat.AuthService.Consumers.Kafka;

public sealed class UserEmailConfirmedSubscriber(
    IAuthInternalApiClient authInternalApiClient,
    ILogger<UserEmailConfirmedSubscriber> logger)
{
    [Subscribe]
    public async Task HandleAsync(
        UserEmailConfirmedIntegrationEvent message,
        CancellationToken cancellationToken)
    {
        if (message.Email is not { Address: not null } email)
        {
            throw new NonTransientException("Payload does not contain Email.Address.");
        }

        var emailAddress = email.Address.Trim();
        if (string.IsNullOrWhiteSpace(emailAddress))
        {
            throw new NonTransientException("Payload does not contain Email.Address.");
        }

        if (!email.IsAuth)
        {
            logger.LogDebug(
                "Skipping non-auth email confirmation for user profile {UserProfileId}, email {EmailId}.",
                message.UserProfileId,
                message.EmailId);
            return;
        }

        try
        {
            await authInternalApiClient.ConfirmEmailAsync(
                new AuthEmailConfirmationRequest
                {
                    EmailAddress = emailAddress
                },
                cancellationToken);
        }
        catch (NonTransientException exception)
        {
            logger.LogInformation(
                exception,
                "Skipping auth email confirmation for user profile {UserProfileId}, email {EmailId}. Reason: {Reason}",
                message.UserProfileId,
                message.EmailId,
                exception.Message);

            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Transient failure while confirming auth email for user profile {UserProfileId}, email {EmailId}.",
                message.UserProfileId,
                message.EmailId);

            throw;
        }
    }
}
