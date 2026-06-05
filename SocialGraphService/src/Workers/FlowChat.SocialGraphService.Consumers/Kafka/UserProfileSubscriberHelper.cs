using AutoMapper;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.UserProfileService.ReadModels;
using FlowChat.SocialGraphService.Consumers.SocialGraph.Contracts;

namespace FlowChat.SocialGraphService.Consumers.Kafka;

internal static class UserProfileSubscriberHelper
{
    public static BulkUpsertOrDeleteUserProfileProjectionRequestItem MapProjectionEvent(
        ProjectionIntegrationEvent<UserProfileReadModel> message,
        IMapper mapper)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(mapper);

        if (message.Version <= 0)
            throw new NonTransientException("Payload does not contain valid SourceVersion.");

        try
        {
            return message.Operation switch
            {
                OperationType.Created or OperationType.Updated => CreateUpsertItem(message, mapper),
                OperationType.Deleted => CreateDeleteItem(message),
                _ => throw new NonTransientException($"Unsupported user profile projection operation {message.Operation}.")
            };
        }
        catch (AutoMapperMappingException exception) when (exception.InnerException is NonTransientException nonTransientException)
        {
            throw nonTransientException;
        }
    }

    public static BulkUpsertOrDeleteUserProfileProjectionRequest CreateBulkUpsertOrDeleteRequest(
        IReadOnlyCollection<BulkUpsertOrDeleteUserProfileProjectionRequestItem> items) =>
        new() { Items = KeepLastItemPerUserProfile(items) };

    private static IReadOnlyCollection<BulkUpsertOrDeleteUserProfileProjectionRequestItem> KeepLastItemPerUserProfile(
        IReadOnlyCollection<BulkUpsertOrDeleteUserProfileProjectionRequestItem> items)
    {
        // Kafka batch can contain multiple events for one profile, while the bulk endpoint rejects duplicate keys
        return items
            .Select((item, index) => new { item, index })
            .GroupBy(x => x.item.UserProfileId)
            .Select(group => group
                .OrderBy(x => x.item.SourceVersion)
                .ThenBy(x => x.index)
                .Last())
            .OrderBy(x => x.index)
            .Select(x => x.item)
            .ToArray();
    }

    private static Guid ResolveUserProfileId(Guid userProfileId, string fieldName) =>
        userProfileId != Guid.Empty
            ? userProfileId
            : throw new NonTransientException($"Payload does not contain valid {fieldName}.");

    private static BulkUpsertOrDeleteUserProfileProjectionRequestItem CreateUpsertItem(
        ProjectionIntegrationEvent<UserProfileReadModel> message,
        IMapper mapper)
    {
        var userProfileId = ResolveUserProfileId(message.Value.UserProfileId, nameof(message.Value.UserProfileId));
        var value = mapper.Map<UserProfileProjectionRequest>(message.Value);
        value.SourceVersion = message.Version;

        return new BulkUpsertOrDeleteUserProfileProjectionRequestItem
        {
            UserProfileId = userProfileId,
            SourceVersion = message.Version,
            Value = value
        };
    }

    private static BulkUpsertOrDeleteUserProfileProjectionRequestItem CreateDeleteItem(
        ProjectionIntegrationEvent<UserProfileReadModel> message)
    {
        var userProfileId = ResolveUserProfileId(message.SourceAggregateId, nameof(message.SourceAggregateId));

        return new BulkUpsertOrDeleteUserProfileProjectionRequestItem
        {
            UserProfileId = userProfileId,
            SourceVersion = message.Version,
            Value = null
        };
    }
}
