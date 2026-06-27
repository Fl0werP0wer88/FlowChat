using FlowChat.HarnessService.Application.Contracts.Infrastructure;
using Silverback;
using Silverback.Messaging.Consuming.KafkaOffsetStore;

namespace FlowChat.HarnessService.Infrastructure.Kafka;

// Delegates to Silverback's own KafkaOffsetStoreScope (set on the ambient SilverbackContext
// for the current message/batch by KafkaOffsetStoreConsumerBehavior) instead of upserting
// SilverbackStoredOffset by hand. Calling this while our EF transaction is still enlisted
// (i.e. before the command handler's transaction commits) makes EntityFrameworkKafkaOffsetStore
// attach to the same underlying DbTransaction via UseTransactionIfAvailable, so the offset
// commit and the projection upsert succeed or roll back together.
public sealed class ConsumerOffsetStore(ISilverbackContext silverbackContext) : IConsumerOffsetStore
{
    public Task CommitConsumedOffsetsAsync(CancellationToken cancellationToken) =>
        silverbackContext.GetKafkaOffsetStoreScope().StoreOffsetsAsync();
}

// Registered for the API host, which never runs BulkUpsertProjectionCommand (it has no
// Silverback/Kafka connection, so ISilverbackContext isn't available there).
public sealed class NullConsumerOffsetStore : IConsumerOffsetStore
{
    public Task CommitConsumedOffsetsAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
