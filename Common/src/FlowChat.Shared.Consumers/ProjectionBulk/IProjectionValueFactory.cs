using FlowChat.Core.Messaging;

namespace FlowChat.Shared.Consumers.ProjectionBulk;

public interface IProjectionValueFactory<TReadModel, TValue, TKey>
    where TReadModel : class
    where TValue : class
    where TKey : notnull
{
    TValue MapValue(ProjectionIntegrationEvent<TReadModel> message);

    // Override when one inbound message must fan out to multiple projected rows (e.g. deriving both directions of a symmetric relationship).
    IEnumerable<TValue> MapValues(ProjectionIntegrationEvent<TReadModel> message) => [MapValue(message)];

    TKey GetDeduplicationKey(TValue value);
}
