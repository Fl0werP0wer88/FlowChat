using FlowChat.Core.Contracts;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.SocialGraphService.ReadModels;

namespace FlowChat.SocialGraphService.Infrastructure.Configuration.Settings;

public sealed class ContactProjectionProducerSettingsSection
    : ProducerSettingsSectionBase,
        IKafkaProducerSettingsSection<ProjectionIntegrationEvent<ContactReadModel>>
{
    public override string SectionName => "Kafka:ContactProjectionProducer";

    public override string BootstrapServers { get; set; } = "localhost:9092";

    public override string Topic { get; set; } = "dev.flowchat.social-graph.contact-projection.v1";
}
