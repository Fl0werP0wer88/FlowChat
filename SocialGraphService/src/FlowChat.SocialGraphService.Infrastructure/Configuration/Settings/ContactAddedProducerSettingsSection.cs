using FlowChat.Core.Messaging.SocialGraphService.Events;

using FlowChat.Core.Contracts;

namespace FlowChat.SocialGraphService.Infrastructure.Configuration.Settings;

public sealed class ContactAddedProducerSettingsSection : SettingsSectionBase, IKafkaProducerSettingsSection<ContactAddedIntegrationEvent>
{
    public override string SectionName => "Kafka:ContactAddedProducer";

    public string BootstrapServers { get; set; } = "localhost:9092";

    public string Topic { get; set; } = "dev.flowchat.social-graph.contact";
}
