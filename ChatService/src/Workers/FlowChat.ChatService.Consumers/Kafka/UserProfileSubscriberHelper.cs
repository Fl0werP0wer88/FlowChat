using AutoMapper;
using FlowChat.ChatService.Consumers.ChatService.Contracts;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.UserProfileService.Events;

namespace FlowChat.ChatService.Consumers.Kafka;

internal static class UserProfileSubscriberHelper
{
    public static BulkUpsertOrDeleteUserProfileProjectionRequestItem? MapAndFilterEvents(IntegrationEvent message, IMapper mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        try
        {
            return message switch
            {
                UserProfileCreatedIntegrationEvent created => new BulkUpsertOrDeleteUserProfileProjectionRequestItem
                {
                    UserProfileId = created.UserProfileId,
                    Value = mapper.Map<UserProfileProjectionRequest>(created)
                },
                UserProfileChangedIntegrationEvent changed => new BulkUpsertOrDeleteUserProfileProjectionRequestItem
                {
                    UserProfileId = changed.UserProfileId,
                    Value = mapper.Map<UserProfileProjectionRequest>(changed)
                },
                UserProfileDeletedIntegrationEvent deleted => new BulkUpsertOrDeleteUserProfileProjectionRequestItem
                {
                    UserProfileId = deleted.UserProfileId,
                    Value = null
                },
                _ => null
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
            .Select(group => group.Last())
            .OrderBy(x => x.index)
            .Select(x => x.item)
            .ToArray();
    }
}
