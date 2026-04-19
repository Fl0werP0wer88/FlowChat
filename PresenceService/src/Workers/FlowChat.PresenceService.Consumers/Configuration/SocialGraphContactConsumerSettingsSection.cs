using FlowChat.Shared.Infrastructure.Silverback.Kafka;

using FlowChat.Core.Contracts;

namespace FlowChat.PresenceService.Consumers.Configuration;

public sealed class SocialGraphContactConsumerSettingsSection : SettingsSectionBase, IRetryableKafkaConsumerSettingsSection
{
    public override string SectionName => "Kafka:SocialGraphContactConsumer";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string GroupId { get; set; } = "presence-service";
    public string RetryGroupId { get; set; } = "presence-service-social-graph-contact-retry";
    public string Topic { get; set; } = "dev.flowchat.social-graph.contact";
    public string RetryTopic { get; set; } = "dev.flowchat.social-graph.contact.retry";
    public string DeadLetterTopic { get; set; } = "dev.flowchat.social-graph.contact.dlq";
    public int MaxRetryCount { get; set; } = 5;
    public int RetryBaseDelaySeconds { get; set; } = 5;
    public int RetryMaxDelaySeconds { get; set; } = 300;
    public string AutoOffsetReset { get; set; } = "Earliest";
}
