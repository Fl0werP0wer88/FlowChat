using FlowChat.Core.Contracts;
using FlowChat.Shared.Consumers.ProjectionBulk;

namespace FlowChat.PresenceService.Consumers.Configuration.Settings;

public sealed class SocialGraphContactConsumerSettingsSection : SettingsSectionBase, IProjectionBulkConsumerSettingsSection
{
    public override string SectionName => "Kafka:SocialGraphContactConsumer";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string GroupId { get; set; } = "presence-service";
    public string RetryGroupId { get; set; } = "presence-service-social-graph-contact-retry";
    public string Topic { get; set; } = "dev.flowchat.social-graph.contact-projection.v1";
    public string RetryTopic { get; set; } = "dev.flowchat.social-graph.contact-projection.v1.presence-service.retry";
    public string DeadLetterTopic { get; set; } = "dev.flowchat.social-graph.contact-projection.v1.presence-service.dlq";
    public int MaxRetryCount { get; set; } = 5;
    public int RetryBaseDelaySeconds { get; set; } = 5;
    public int RetryMaxDelaySeconds { get; set; } = 300;
    public string AutoOffsetReset { get; set; } = "Earliest";
    public int BatchSize { get; set; } = 100;
    public int BatchMaxWaitTimeMilliseconds { get; set; } = 1000;
}
