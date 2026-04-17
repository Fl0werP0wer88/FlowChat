using FlowChat.Core.Contracts;

namespace FlowChat.SocialGraphService.OutboxPublisher.Configuration;

public sealed class ContactAddedProducerSettingsSection : SettingsSectionBase
{
    public override string SectionName => "Kafka:ContactAddedProducer";

    public string BootstrapServers { get; set; } = "localhost:9092";

    public string Topic { get; set; } = "dev.flowchat.social-graph.contact";
}
