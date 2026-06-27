using Silverback.Configuration;

namespace FlowChat.Shared.Consumers.ProjectionBulk;

public static class ProjectionBulkServiceCollectionExtensions
{
    public static SilverbackBuilder AddProjectionBulk(
        this SilverbackBuilder builder,
        IProjectionBulkConsumerSettingsSection options,
        string mainConsumerName,
        string retryConsumerName,
        Action<ProjectionBulkBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(mainConsumerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(retryConsumerName);
        ArgumentNullException.ThrowIfNull(configure);

        configure(new ProjectionBulkBuilder(
            builder,
            options,
            mainConsumerName,
            retryConsumerName));

        return builder;
    }
}
