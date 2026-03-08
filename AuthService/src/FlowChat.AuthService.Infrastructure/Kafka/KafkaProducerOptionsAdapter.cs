namespace FlowChat.AuthService.Infrastructure.Kafka;

public sealed class KafkaProducerOptionsAdapter<TEvent> : IKafkaProducerOptions<TEvent>
{
    public KafkaProducerOptionsAdapter(IKafkaProducerOptions source)
    {
        ArgumentNullException.ThrowIfNull(source);

        BootstrapServers = source.BootstrapServers;
        Topic = source.Topic;
    }

    public string BootstrapServers { get; set; }

    public string Topic { get; set; }
}
