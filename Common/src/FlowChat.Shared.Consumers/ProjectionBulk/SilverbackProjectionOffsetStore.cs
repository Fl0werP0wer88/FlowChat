using FlowChat.Shared.Application;
using Silverback;
using Silverback.Messaging.Consuming.KafkaOffsetStore;

namespace FlowChat.Shared.Consumers.ProjectionBulk;

public sealed class SilverbackProjectionOffsetStore(ISilverbackContext silverbackContext) : IProjectionOffsetStore
{
    public Task CommitConsumedOffsetsAsync(CancellationToken cancellationToken) =>
        silverbackContext.GetKafkaOffsetStoreScope().StoreOffsetsAsync();
}
