using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.SocialGraphService.ReadModels;
using FlowChat.Core.Exceptions;
using FlowChat.PresenceService.Consumers.Presence.Contracts;

namespace FlowChat.PresenceService.Consumers.Kafka;

internal static class ContactProjectionSubscriberHelper
{
    private const string ProjectionSource = "social-graph-contact-events";

    public static BulkUpsertOrDeleteUserContactProjectionRequestItem MapProjectionEvent(
        ProjectionIntegrationEvent<ContactReadModel> message)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (message.SourceAggregateVersion <= 0)
            throw new NonTransientException("Payload does not contain valid SourceVersion.");

        var observedUserId = ResolveUserId(message.Value.ContactUserId, nameof(message.Value.ContactUserId));
        var observerUserId = ResolveUserId(message.Value.OwnerUserId, nameof(message.Value.OwnerUserId));

        return message.Operation switch
        {
            OperationType.Created or OperationType.Updated => new BulkUpsertOrDeleteUserContactProjectionRequestItem
            {
                ObservedUserId = observedUserId,
                ObserverUserId = observerUserId,
                SourceVersion = message.SourceAggregateVersion,
                SourceCreatedAtUtc = message.SourceAggregateCreatedAtUtc,
                SourceLastModifiedAtUtc = message.SourceAggregateModifiedAtUtc,
                SourceDeletedAtUtc = message.SourceAggregateDeletedAt,
                Value = new UserContactProjectionRequest { Source = ProjectionSource }
            },
            OperationType.Deleted => new BulkUpsertOrDeleteUserContactProjectionRequestItem
            {
                ObservedUserId = observedUserId,
                ObserverUserId = observerUserId,
                SourceVersion = message.SourceAggregateVersion,
                SourceCreatedAtUtc = message.SourceAggregateCreatedAtUtc,
                SourceLastModifiedAtUtc = message.SourceAggregateModifiedAtUtc,
                SourceDeletedAtUtc = message.SourceAggregateDeletedAt,
                Value = null
            },
            _ => throw new NonTransientException($"Unsupported contact projection operation {message.Operation}.")
        };
    }

    public static BulkUpsertOrDeleteUserContactProjectionRequest CreateBulkUpsertOrDeleteRequest(
        IReadOnlyCollection<BulkUpsertOrDeleteUserContactProjectionRequestItem> items) =>
        new() { Items = KeepLastItemPerContactObserver(items) };

    private static IReadOnlyCollection<BulkUpsertOrDeleteUserContactProjectionRequestItem> KeepLastItemPerContactObserver(
        IReadOnlyCollection<BulkUpsertOrDeleteUserContactProjectionRequestItem> items)
    {
        return items
            .Select((item, index) => new { item, index })
            .GroupBy(x => new { x.item.ObservedUserId, x.item.ObserverUserId })
            .Select(group => group
                .OrderBy(x => x.item.SourceVersion)
                .ThenBy(x => x.index)
                .Last())
            .OrderBy(x => x.index)
            .Select(x => x.item)
            .ToArray();
    }

    private static Guid ResolveUserId(Guid userId, string fieldName) =>
        userId != Guid.Empty
            ? userId
            : throw new NonTransientException($"Payload does not contain valid {fieldName}.");
}
