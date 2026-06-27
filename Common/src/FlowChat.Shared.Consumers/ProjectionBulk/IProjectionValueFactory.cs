using FlowChat.Core.Messaging;

namespace FlowChat.Shared.Consumers.ProjectionBulk;

public interface IProjectionValueFactory<TReadModel, TValue>
    where TReadModel : class
    where TValue : class
{
    TValue MapValue(ProjectionIntegrationEvent<TReadModel> message);
}
