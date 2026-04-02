namespace FlowChat.Shared.Infrastructure.Silverback.Kafka;

public interface IKafkaProducerOptions
{
    string BootstrapServers { get; set; }
    string Topic { get; set; }
}

public interface IKafkaProducerOptions<TEvent> : IKafkaProducerOptions
{
}
