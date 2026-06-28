using FlowChat.Core.Messaging;

namespace FlowChat.Shared.Consumers.ProjectionBulk;

public interface IProjectionValueFactory<TReadModel, TValue, TKey>
    where TReadModel : class
    where TValue : class
    where TKey : notnull
{
    TValue MapValue(ProjectionIntegrationEvent<TReadModel> message);

    TKey GetDeduplicationKey(TValue value);
}
