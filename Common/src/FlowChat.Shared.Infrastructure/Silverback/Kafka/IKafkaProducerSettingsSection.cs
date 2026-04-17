namespace FlowChat.Shared.Infrastructure.Silverback.Kafka;

public interface IKafkaProducerSettingsSection
{
    string BootstrapServers { get; set; }
    string Topic { get; set; }
}

public interface IKafkaProducerSettingsSection<TEvent> : IKafkaProducerSettingsSection
{
}
