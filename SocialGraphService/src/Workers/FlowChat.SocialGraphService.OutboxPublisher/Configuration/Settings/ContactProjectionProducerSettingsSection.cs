using FlowChat.Core.Contracts;

namespace FlowChat.SocialGraphService.OutboxPublisher.Configuration.Settings;

public sealed class ContactProjectionProducerSettingsSection : ProducerSettingsSectionBase
{
    public override string SectionName => "Kafka:ContactProjectionProducer";

    public override string BootstrapServers { get; set; } = "localhost:9092";

    public override string Topic { get; set; } = "dev.flowchat.social-graph.contact-projection.v1";
}
