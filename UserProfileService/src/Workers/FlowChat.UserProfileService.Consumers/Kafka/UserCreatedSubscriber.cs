using FlowChat.Core.Exceptions;
using FlowChat.Messaging.Contracts.AuthService.Events;
using FlowChat.UserProfileService.Consumers.Services;
using FlowChat.UserProfileService.Consumers.UserProfileApi.Contracts;
using Silverback.Messaging.Subscribers;

namespace FlowChat.UserProfileService.Consumers.Kafka;

public sealed class UserCreatedSubscriber(
    IUserProfileInternalApiClient userProfileInternalApiClient,
    ILogger<UserCreatedSubscriber> logger)
{
    [Subscribe]
    public async Task HandleAsync(
        UserCreatedIntegrationEvent message,
        CancellationToken cancellationToken)
    {
        var userName = message.UserName?.Trim();
        if (string.IsNullOrWhiteSpace(userName))
        {
            throw new NonTransientException("Payload does not contain UserName.");
        }

        var userId = ResolveUserId(message.UserId);
        if (!userId.HasValue)
        {
            throw new NonTransientException("Payload does not contain valid UserId.");
        }

        var displayName = ResolveDisplayName(message, userName);

        try
        {
            await userProfileInternalApiClient.CreateInitialUserProfileAsync(
                new CreateInitialUserProfileRequest
                {
                    UserName = userName,
                    DisplayName = displayName,
                    AvatarUrl = null,
                    Bio = null,
                    Email = message.Email,
                    Phone = message.PhoneNumber,
                    UserId = userId.Value
                },
                cancellationToken);
        }
        catch (NonTransientException ex)
        {
            logger.LogInformation(
                ex,
                "Skipping user profile creation for user {UserId}. Reason: {Reason}",
                userId.Value,
                ex.Message);

            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Transient failure while creating user profile for user {UserId}.",
                userId.Value);

            throw;
        }
    }

    private static Guid? ResolveUserId(Guid payloadUserId) =>
        payloadUserId != Guid.Empty
            ? payloadUserId
            : null;

    private static string ResolveDisplayName(UserCreatedIntegrationEvent message, string userName)
    {
        if (!string.IsNullOrWhiteSpace(message.DisplayName))
        {
            return message.DisplayName.Trim();
        }

        var firstName = message.FirstName?.Trim();
        var lastName = message.LastName?.Trim();
        var fullName = $"{firstName} {lastName}".Trim();

        return string.IsNullOrWhiteSpace(fullName)
            ? userName
            : fullName;
    }
}
