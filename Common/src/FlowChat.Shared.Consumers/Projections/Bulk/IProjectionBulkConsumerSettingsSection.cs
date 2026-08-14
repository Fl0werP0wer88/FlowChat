namespace FlowChat.Shared.Consumers.Projections.Bulk;

public interface IProjectionBulkConsumerSettingsSection
{
    string BootstrapServers { get; }

    string GroupId { get; }

    string Topic { get; }

    string AutoOffsetReset { get; }

    int BatchSize { get; }

    int BatchMaxWaitTimeMilliseconds { get; }
}
