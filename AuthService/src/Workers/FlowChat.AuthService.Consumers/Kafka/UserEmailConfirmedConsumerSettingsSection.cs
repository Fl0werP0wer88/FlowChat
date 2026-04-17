using FlowChat.Shared.Infrastructure.Silverback.Kafka;

namespace FlowChat.AuthService.Consumers.Kafka;

public sealed class UserEmailConfirmedConsumerSettingsSection : IRetryableKafkaConsumerOptions
{
    public const string SectionName = "Kafka:UserEmailConfirmedConsumer";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string GroupId { get; set; } = "auth-service";
    public string RetryGroupId { get; set; } = "auth-service-retry";
    public string Topic { get; set; } = "dev.flowchat.user-profile.user-profile.v1";
    public string RetryTopic { get; set; } = "dev.flowchat.user-profile.user-profile.v1.retry";
    public string DeadLetterTopic { get; set; } = "dev.flowchat.user-profile.user-profile.v1.dlq";
    public int MaxRetryCount { get; set; } = 5;
    public int RetryBaseDelaySeconds { get; set; } = 5;
    public int RetryMaxDelaySeconds { get; set; } = 300;
    public string AutoOffsetReset { get; set; } = "Earliest";
}
