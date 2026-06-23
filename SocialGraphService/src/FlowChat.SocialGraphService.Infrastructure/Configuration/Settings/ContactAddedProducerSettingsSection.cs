using FlowChat.Core.Messaging.SocialGraphService.Events;

using FlowChat.Core.Contracts;

namespace FlowChat.SocialGraphService.Infrastructure.Configuration.Settings;

public sealed class ContactAddedProducerSettingsSection : ProducerSettingsSectionBase, IKafkaProducerSettingsSection<ContactAddedIntegrationEvent>
{
    public override string SectionName => "Kafka:ContactAddedProducer";

    public override string BootstrapServers { get; set; } = "localhost:9092";

    public override string Topic { get; set; } = "dev.flowchat.social-graph.contact";
}
