using FlowChat.Core.Messaging;

namespace FlowChat.Shared.Consumers.ProjectionBulk;

public interface IProjectionCommandItemFactory<TReadModel, TItem>
    where TReadModel : class
    where TItem : notnull
{
    TItem MapItem(ProjectionIntegrationEvent<TReadModel> message);
}
