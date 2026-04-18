using FlowChat.Core.Contracts;
using FlowChat.Core.Messaging;

namespace FlowChat.Shared.Infrastructure.Silverback.Kafka;

public interface IKafkaProducerSettingsSection : ISettingSection
{
    string BootstrapServers { get; }
    string Topic { get; }
}

public interface IKafkaProducerSettingsSection<TEvent> : IKafkaProducerSettingsSection
{
}