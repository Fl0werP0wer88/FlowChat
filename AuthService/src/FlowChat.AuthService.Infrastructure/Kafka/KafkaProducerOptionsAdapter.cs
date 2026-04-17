using FlowChat.Shared.Infrastructure.Silverback.Kafka;

namespace FlowChat.AuthService.Infrastructure.Kafka;

public sealed class KafkaProducerOptionsAdapter<TEvent> : IKafkaProducerSettingsSection<TEvent>
{
    public KafkaProducerOptionsAdapter(IKafkaProducerSettingsSection source)
    {
        ArgumentNullException.ThrowIfNull(source);

        BootstrapServers = source.BootstrapServers;
        Topic = source.Topic;
    }

    public string BootstrapServers { get; set; }

    public string Topic { get; set; }
}
