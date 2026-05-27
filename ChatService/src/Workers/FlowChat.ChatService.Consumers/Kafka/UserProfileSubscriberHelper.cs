using FlowChat.ChatService.Consumers.ChatService.Contracts;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.UserProfileService.Events;

namespace FlowChat.ChatService.Consumers.Kafka;

internal static class UserProfileSubscriberHelper
{
    private const string ProjectionSource = "user-profile-events";

    public static UserProfileProjectionRequest? Map(IntegrationEvent message) =>
        message switch
        {
            UserProfileCreatedIntegrationEvent created => Map(created),
            UserProfileChangedIntegrationEvent changed => Map(changed),
            _ => null
        };

    public static BulkUpsertUserProfileProjectionRequest CreateBulkUpsertRequest(
        IReadOnlyCollection<UserProfileProjectionRequest> items) =>
        new() { Items = KeepLastItemPerUserProfile(items) };

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
            AvatarUrl = NormalizeOptional(avatarUrl),
            Source = ProjectionSource
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

    private static IReadOnlyCollection<UserProfileProjectionRequest> KeepLastItemPerUserProfile(
        IReadOnlyCollection<UserProfileProjectionRequest> items)
    {
        // Kafka batch can contain multiple events for one profile, while the bulk endpoint rejects duplicate keys
        return items
            .Select((item, index) => new { item, index })
            .GroupBy(x => x.item.UserProfileId)
            .Select(group => group.Last())
            .OrderBy(x => x.index)
            .Select(x => x.item)
            .ToArray();
    }

    private static string? ComputeDisplayName(string? firstName, string? lastName)
    {
        var parts = new[] { firstName?.Trim(), lastName?.Trim() }
            .Where(p => !string.IsNullOrEmpty(p));
        var name = string.Join(" ", parts);
        return string.IsNullOrEmpty(name) ? null : name;
    }
}
