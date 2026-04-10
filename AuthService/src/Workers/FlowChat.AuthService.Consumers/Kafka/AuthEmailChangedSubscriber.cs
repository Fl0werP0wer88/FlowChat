using FlowChat.AuthService.Consumers.AuthApi.Contracts;
using FlowChat.AuthService.Consumers.Services;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.Shared.Infrastructure.Silverback.Subscribers;

namespace FlowChat.AuthService.Consumers.Kafka;

public sealed class AuthEmailChangedSubscriber(
    IAuthInternalApiClient authInternalApiClient,
    ILogger<AuthEmailChangedSubscriber> logger)
    : SubscriberBase<AuthEmailChangedIntegrationEvent>(logger)
{
    protected override async Task ExecuteAsync(
        AuthEmailChangedIntegrationEvent message,
        CancellationToken cancellationToken)
    {
        if (message.UserProfileId == Guid.Empty)
        {
            throw new NonTransientException("Payload does not contain valid UserProfileId.");
        }

        var emailAddress = message.EmailAddress?.Trim();
        if (string.IsNullOrWhiteSpace(emailAddress))
        {
            throw new NonTransientException("Payload does not contain EmailAddress.");
        }

        await authInternalApiClient.ChangeAuthEmailAsync(
            new AuthEmailChangeRequest
            {
                UserId = message.UserProfileId,
                EmailAddress = emailAddress
            },
            cancellationToken);
    }
}
