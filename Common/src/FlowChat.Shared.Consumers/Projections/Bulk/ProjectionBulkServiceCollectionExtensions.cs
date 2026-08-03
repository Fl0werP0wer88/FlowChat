using Silverback.Configuration;

namespace FlowChat.Shared.Consumers.Projections.Bulk;

public static class ProjectionBulkServiceCollectionExtensions
{
    public static SilverbackBuilder AddProjectionBulk(
        this SilverbackBuilder builder,
        IProjectionBulkConsumerSettingsSection options,
        string mainConsumerName,
        Action<ProjectionBulkBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(mainConsumerName);
        ArgumentNullException.ThrowIfNull(configure);

        configure(new ProjectionBulkBuilder(
            builder,
            options,
            mainConsumerName));

        return builder;
    }
}
