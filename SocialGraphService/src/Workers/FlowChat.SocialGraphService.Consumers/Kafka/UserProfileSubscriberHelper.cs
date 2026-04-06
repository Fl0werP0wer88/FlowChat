using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.SocialGraphService.Consumers.Services;
using FlowChat.SocialGraphService.Consumers.SocialGraph.Contracts;
using Microsoft.Extensions.Logging;

namespace FlowChat.SocialGraphService.Consumers.Kafka;

internal static class UserProfileSubscriberHelper
{
    public static async Task UpsertAsync(
        ISocialGraphInternalApiClient socialGraphInternalApiClient,
        ILogger logger,
        UpsertUserProfileProjectionRequest request,
        string eventType,
        CancellationToken cancellationToken)
    {
        await socialGraphInternalApiClient.UpsertUserProfileProjectionAsync(request, cancellationToken);

        logger.LogInformation(
            "Upserted user profile projection for profile {UserProfileId} from {EventType}.",
            request.UserProfileId,
            eventType);
    }

    public static UpsertUserProfileProjectionRequest Map(UserProfileCreatedIntegrationEvent message) =>
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

    public static UpsertUserProfileProjectionRequest Map(UserProfileStateChangedIntegrationEvent message) =>
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
