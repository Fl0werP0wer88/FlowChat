namespace FlowChat.Core.Contracts;

public interface IKafkaProducerSettingsSection : ISettingSection
{
    string BootstrapServers { get; }
    string Topic { get; }
}

public interface IKafkaProducerSettingsSection<TEvent> : IKafkaProducerSettingsSection
{
}
