using FlowChat.Shared.Infrastructure.Silverback.Kafka;

namespace FlowChat.Shared.Consumers.ProjectionBulk;

public interface IProjectionBulkConsumerSettingsSection : IRetryableKafkaConsumerSettingsSection
{
    string BootstrapServers { get; }

    string GroupId { get; }

    string RetryGroupId { get; }

    string AutoOffsetReset { get; }

    int BatchSize { get; }

    int BatchMaxWaitTimeMilliseconds { get; }
}
