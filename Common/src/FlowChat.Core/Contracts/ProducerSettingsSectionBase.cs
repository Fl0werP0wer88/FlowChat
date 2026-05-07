namespace FlowChat.Core.Contracts;

public abstract class ProducerSettingsSectionBase : SettingsSectionBase, IKafkaProducerSettingsSection
{
    public abstract string BootstrapServers { get; set; }

    public abstract string Topic { get; set; }
}
