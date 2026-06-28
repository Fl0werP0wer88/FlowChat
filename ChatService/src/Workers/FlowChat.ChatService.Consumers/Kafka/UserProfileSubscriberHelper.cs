using AutoMapper;
using FlowChat.ChatService.Application.Features.UserProfile;
using FlowChat.ChatService.Application.Features.UserProfile.Commands.BulkUpsertOrDeleteUserProfileProjection;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.UserProfileService.ReadModels;
using FlowChat.Shared.Domain;

namespace FlowChat.ChatService.Consumers.Kafka;

internal static class UserProfileSubscriberHelper
{
    public static UserProfileProjectionCommandItem MapProjectionEvent(
        ProjectionIntegrationEvent<UserProfileReadModel> message,
        IMapper mapper)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(mapper);

        if (message.SourceAggregateVersion <= 0)
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

    public static IReadOnlyCollection<UserProfileProjectionCommandItem> KeepLastItemPerUserProfile(
        IReadOnlyCollection<UserProfileProjectionCommandItem> items)
    {
        // Kafka batch can contain multiple events for one profile, while the bulk command rejects duplicate keys
        return items
            .Select((item, index) => new { item, index })
            .GroupBy(x => x.item.EntityId.Value)
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

    private static UserProfileProjectionCommandItem CreateUpsertItem(
        ProjectionIntegrationEvent<UserProfileReadModel> message,
        IMapper mapper)
    {
        var userProfileId = ResolveUserId(message.Value.UserProfileId, nameof(message.Value.UserProfileId));
        var value = mapper.Map<UserProfileProjectionDto>(message.Value);
        value.SourceVersion = message.SourceAggregateVersion;

        return new UserProfileProjectionCommandItem(
            Id<UserProfileProjectionDto>.FromGuid(userProfileId),
            value,
            message.Operation,
            message.SourceAggregateVersion,
            message.SourceAggregateCreatedAtUtc,
            message.SourceAggregateModifiedAtUtc,
            message.SourceAggregateDeletedAt);
    }

    private static UserProfileProjectionCommandItem CreateDeleteItem(
        ProjectionIntegrationEvent<UserProfileReadModel> message)
    {
        var userProfileId = ResolveUserId(message.SourceAggregateId, nameof(message.SourceAggregateId));

        return new UserProfileProjectionCommandItem(
            Id<UserProfileProjectionDto>.FromGuid(userProfileId),
            null,
            message.Operation,
            message.SourceAggregateVersion,
            message.SourceAggregateCreatedAtUtc,
            message.SourceAggregateModifiedAtUtc,
            message.SourceAggregateDeletedAt);
    }
}
