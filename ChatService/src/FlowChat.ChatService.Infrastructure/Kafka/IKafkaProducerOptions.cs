namespace FlowChat.ChatService.Infrastructure.Kafka;

public interface IKafkaProducerOptions
{
    string BootstrapServers { get; set; }
    string Topic { get; set; }
}

public interface IKafkaProducerOptions<TEvent> : IKafkaProducerOptions
{
}
