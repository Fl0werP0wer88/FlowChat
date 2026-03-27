using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.SocialGraphService.Consumers.Services;
using FlowChat.SocialGraphService.Consumers.SocialGraph.Contracts;
using Silverback.Messaging.Subscribers;

namespace FlowChat.SocialGraphService.Consumers.Kafka;

public sealed class UserProfileSubscriber(
    ISocialGraphInternalApiClient socialGraphInternalApiClient,
    ILogger<UserProfileSubscriber> logger)
{
    [Subscribe]
    public Task HandleAsync(
        UserProfileCreatedIntegrationEvent message,
        CancellationToken cancellationToken) =>
        UpsertAsync(
            Map(message),
            nameof(UserProfileCreatedIntegrationEvent),
            cancellationToken);

    [Subscribe]
    public Task HandleAsync(
        UserProfileStateChangedIntegrationEvent message,
        CancellationToken cancellationToken) =>
        UpsertAsync(
            Map(message),
            nameof(UserProfileStateChangedIntegrationEvent),
            cancellationToken);

    private async Task UpsertAsync(
        UpsertUserProfileReadModelRequest request,
        string eventType,
        CancellationToken cancellationToken)
    {
        try
        {
            await socialGraphInternalApiClient.UpsertUserProfileReadModelAsync(request, cancellationToken);

            logger.LogInformation(
                "Upserted user profile read model for profile {UserProfileId} from {EventType}.",
                request.UserProfileId,
                eventType);
        }
        catch (NonTransientException ex)
        {
            logger.LogInformation(
                ex,
                "Skipping user profile read model upsert for profile {UserProfileId}. Reason: {Reason}",
                request.UserProfileId,
                ex.Message);

            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Transient failure while upserting user profile read model for profile {UserProfileId}.",
                request.UserProfileId);

            throw;
        }
    }

    private static UpsertUserProfileReadModelRequest Map(UserProfileCreatedIntegrationEvent message) =>
        new()
        {
            UserProfileId = ResolveUserProfileId(message.UserProfileId),
            UserName = NormalizeRequired(message.UserName, nameof(message.UserName)),
            DisplayName = NormalizeRequired(message.DisplayName, nameof(message.DisplayName)),
            MainEmail = NormalizeOptional(message.MainEmail),
            MainPhone = NormalizeOptional(message.MainPhone),
            AvatarUrl = NormalizeOptional(message.AvatarUrl),
            Bio = NormalizeOptional(message.Bio),
            IsActive = message.IsActive,
            LastSeenAtUtc = message.LastSeenAtUtc,
            IsEmailVisible = message.IsEmailVisible,
            IsPhoneVisible = message.IsPhoneVisible
        };

    private static UpsertUserProfileReadModelRequest Map(UserProfileStateChangedIntegrationEvent message) =>
        new()
        {
            UserProfileId = ResolveUserProfileId(message.UserProfileId),
            UserName = NormalizeRequired(message.UserName, nameof(message.UserName)),
            DisplayName = NormalizeRequired(message.DisplayName, nameof(message.DisplayName)),
            MainEmail = NormalizeOptional(message.MainEmail),
            MainPhone = NormalizeOptional(message.MainPhone),
            AvatarUrl = NormalizeOptional(message.AvatarUrl),
            Bio = NormalizeOptional(message.Bio),
            IsActive = message.IsActive,
            LastSeenAtUtc = message.LastSeenAtUtc,
            IsEmailVisible = message.IsEmailVisible,
            IsPhoneVisible = message.IsPhoneVisible
        };

    private static Guid ResolveUserProfileId(Guid userProfileId) =>
        userProfileId != Guid.Empty
            ? userProfileId
            : throw new NonTransientException("Payload does not contain valid UserProfileId.");

    private static string NormalizeRequired(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new NonTransientException($"Payload does not contain valid {fieldName}.");
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
