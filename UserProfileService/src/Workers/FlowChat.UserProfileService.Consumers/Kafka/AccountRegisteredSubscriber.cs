using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.Shared.Infrastructure.Silverback.Subscribers;
using FlowChat.UserProfileService.Consumers.Services;
using FlowChat.UserProfileService.Consumers.UserProfileApi.Contracts;

namespace FlowChat.UserProfileService.Consumers.Kafka;

public sealed class AccountRegisteredSubscriber(
    IUserProfileInternalApiClient userProfileInternalApiClient,
    ILogger<AccountRegisteredSubscriber> logger)
    : SubscriberBase<AccountRegisteredIntegrationEvent>(logger)
{
    protected override async Task ExecuteAsync(
        AccountRegisteredIntegrationEvent message,
        CancellationToken cancellationToken)
    {
        var friendlyUserId = message.FriendlyUserId?.Trim();
        if (string.IsNullOrWhiteSpace(friendlyUserId))
        {
            throw new NonTransientException("Payload does not contain FriendlyUserId.");
        }

        var userId = ResolveUserId(message.UserId);
        if (!userId.HasValue)
        {
            throw new NonTransientException("Payload does not contain valid UserId.");
        }

        var displayName = ResolveDisplayName(message, friendlyUserId);

        await userProfileInternalApiClient.CreateInitialUserProfileAsync(
            new CreateInitialUserProfileRequest
            {
                FriendlyUserId = friendlyUserId,
                DisplayName = displayName,
                FirstName = NormalizeOptional(message.FirstName),
                LastName = NormalizeOptional(message.LastName),
                Organization = NormalizeOptional(message.Organization),
                Email = message.Email,
                UserId = userId.Value
            },
            cancellationToken);
    }

    private static Guid? ResolveUserId(Guid payloadUserId) =>
        payloadUserId != Guid.Empty
            ? payloadUserId
            : null;

    private static string ResolveDisplayName(AccountRegisteredIntegrationEvent message, string friendlyUserId)
    {
        var firstName = NormalizeOptional(message.FirstName);
        var lastName = NormalizeOptional(message.LastName);
        var fullName = $"{firstName} {lastName}".Trim();

        if (!string.IsNullOrWhiteSpace(fullName))
        {
            return fullName;
        }

        return friendlyUserId;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
