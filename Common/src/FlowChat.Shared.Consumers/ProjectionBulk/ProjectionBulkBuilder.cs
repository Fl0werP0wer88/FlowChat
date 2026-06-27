using FlowChat.Shared.Application;
using Microsoft.EntityFrameworkCore;
using Silverback.Configuration;

namespace FlowChat.Shared.Consumers.ProjectionBulk;

public sealed class ProjectionBulkBuilder(
    SilverbackBuilder silverbackBuilder,
    IProjectionBulkConsumerSettingsSection options,
    string mainConsumerName,
    string retryConsumerName)
{
    public ProjectionBulkBuilder AddRepository<TItem, TRepository, TImplementation>()
        where TItem : notnull
        where TRepository : class, IProjectionBulkRepository<TItem>
        where TImplementation : class, TRepository
    {
        silverbackBuilder.AddProjectionBulkRepository<TItem, TRepository, TImplementation>();

        return this;
    }

    public ProjectionBulkBuilder AddCommandHandler<TItem, TRepository>()
        where TItem : notnull
        where TRepository : class, IProjectionBulkRepository<TItem>
    {
        silverbackBuilder.AddProjectionBulkCommandHandler<TItem, TRepository>();

        return this;
    }

    public ProjectionBulkBuilder AddConsumer<TDbContext, TReadModel, TItem, TItemFactory>()
        where TDbContext : DbContext
        where TReadModel : class
        where TItem : notnull
        where TItemFactory : class, IProjectionCommandItemFactory<TReadModel, TItem>
    {
        silverbackBuilder.AddProjectionBulkConsumer<TDbContext, TReadModel, TItem, TItemFactory>(
            options,
            mainConsumerName,
            retryConsumerName);

        return this;
    }
}
