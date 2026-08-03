using FlowChat.Core.Messaging;

namespace FlowChat.Shared.Consumers.Projections.Single;

public interface IProjectionSingleValueFactory<TReadModel, TValue>
    where TReadModel : class
    where TValue : class
{
    TValue MapValue(ProjectionIntegrationEvent<TReadModel> message);
}
