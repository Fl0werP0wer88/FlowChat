using FlowChat.PresenceService.Application.Features.ContactObserverProjections;
using FlowChat.PresenceService.Application.Features.ContactObserverProjections.Commands.BulkUpsertOrDeleteUserContactProjection;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.SocialGraphService.ReadModels;
using FlowChat.Core.Exceptions;

namespace FlowChat.PresenceService.Consumers.Kafka;

internal static class ContactProjectionSubscriberHelper
{
    private const string ProjectionSource = "social-graph-contact-events";

    public static UserContactProjectionCommandItem MapProjectionEvent(
        ProjectionIntegrationEvent<ContactReadModel> message)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (message.SourceAggregateVersion <= 0)
            throw new NonTransientException("Payload does not contain valid SourceVersion.");

        var observedUserId = ResolveUserId(message.Value.ContactUserId, nameof(message.Value.ContactUserId));
        var observerUserId = ResolveUserId(message.Value.OwnerUserId, nameof(message.Value.OwnerUserId));

        return message.Operation switch
        {
            OperationType.Created or OperationType.Updated => new UserContactProjectionCommandItem(
                new ContactObserverProjectionDto
                {
                    ObservedUserId = observedUserId,
                    ObserverUserId = observerUserId,
                    Source = ProjectionSource
                },
                message.Operation,
                message.SourceAggregateVersion,
                message.SourceAggregateCreatedAtUtc,
                message.SourceAggregateModifiedAtUtc,
                message.SourceAggregateDeletedAt),
            OperationType.Deleted => new UserContactProjectionCommandItem(
                new ContactObserverProjectionDto
                {
                    ObservedUserId = observedUserId,
                    ObserverUserId = observerUserId,
                    Source = ProjectionSource
                },
                message.Operation,
                message.SourceAggregateVersion,
                message.SourceAggregateCreatedAtUtc,
                message.SourceAggregateModifiedAtUtc,
                message.SourceAggregateDeletedAt),
            _ => throw new NonTransientException($"Unsupported contact projection operation {message.Operation}.")
        };
    }

    public static IReadOnlyCollection<UserContactProjectionCommandItem> KeepLastItemPerContactObserver(
        IReadOnlyCollection<UserContactProjectionCommandItem> items)
    {
        return items
            .Select((item, index) => new { item, index })
            .GroupBy(x => new { x.item.Value.ObservedUserId, x.item.Value.ObserverUserId })
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
