using FlowChat.Core.Contracts;

namespace FlowChat.Shared.Infrastructure.Silverback.Kafka;

public interface IKafkaProducerSettingsSection : ISettingSection
{
    string BootstrapServers { get; set; }
    string Topic { get; set; }
}

public interface IKafkaProducerSettingsSection<TEvent> : IKafkaProducerSettingsSection
{
}
