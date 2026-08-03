using FlowChat.Core.Messaging;

namespace FlowChat.Shared.Consumers.Projection;

public interface IProjectionValueFactory<TReadModel, TValue>
    where TReadModel : class
    where TValue : class
{
    TValue MapValue(ProjectionIntegrationEvent<TReadModel> message);
}
