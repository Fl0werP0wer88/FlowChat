using FlowChat.ChatService.Consumers.ChatService.Contracts;
using FlowChat.ChatService.Consumers.Services;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging.UserProfileService.Events;
using Microsoft.Extensions.Logging;

namespace FlowChat.ChatService.Consumers.Kafka;

internal static class UserProfileSubscriberHelper
{
    public static async Task InsertAsync(
        IChatInternalApiClient apiClient,
        ILogger logger,
        UserProfileProjectionRequest request,
        string eventType,
        CancellationToken cancellationToken)
    {
        await apiClient.InsertUserProfileProjectionAsync(request, cancellationToken);

        logger.LogInformation(
            "Inserted user profile projection for user {UserId} from {EventType}.",
            request.UserProfileId,
            eventType);
    }

    public static async Task UpdateAsync(
        IChatInternalApiClient apiClient,
        ILogger logger,
        UserProfileProjectionRequest request,
        string eventType,
        CancellationToken cancellationToken)
    {
        await apiClient.UpdateUserProfileProjectionAsync(request, cancellationToken);

        logger.LogInformation(
            "Updated user profile projection for user {UserId} from {EventType}.",
            request.UserProfileId,
            eventType);
    }

    public static UserProfileProjectionRequest Map(UserProfileCreatedIntegrationEvent message) =>
        Map(
            message.UserProfileId,
            message.FriendlyUserId,
            message.FirstName,
            message.LastName,
            message.AvatarUrl);

    public static UserProfileProjectionRequest Map(UserProfileChangedIntegrationEvent message) =>
        Map(
            message.UserProfileId,
            message.FriendlyUserId,
            message.FirstName,
            message.LastName,
            message.AvatarUrl);

    private static UserProfileProjectionRequest Map(
        Guid userProfileId,
        string friendlyUserId,
        string? firstName,
        string? lastName,
        string? avatarUrl)
    {
        return new UserProfileProjectionRequest
        {
            UserProfileId = ResolveUserId(userProfileId),
            FriendlyUserId = NormalizeRequired(friendlyUserId, nameof(friendlyUserId)),
            DisplayName = ComputeDisplayName(firstName, lastName),
            AvatarUrl = NormalizeOptional(avatarUrl)
        };
    }

    private static Guid ResolveUserId(Guid userId) =>
        userId != Guid.Empty
            ? userId
            : throw new NonTransientException("Payload does not contain valid UserProfileId.");

    private static string NormalizeRequired(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new NonTransientException($"Payload does not contain valid {fieldName}.");

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? ComputeDisplayName(string? firstName, string? lastName)
    {
        var parts = new[] { firstName?.Trim(), lastName?.Trim() }
            .Where(p => !string.IsNullOrEmpty(p));
        var name = string.Join(" ", parts);
        return string.IsNullOrEmpty(name) ? null : name;
    }
}
