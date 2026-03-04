namespace FlowChat.UserProfileService.Worker.Kafka;

public sealed class UserCreatedConsumerOptions
{
    public const string SectionName = "Kafka:UserCreatedConsumer";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string GroupId { get; set; } = "userprofile-service";
    public string Topic { get; set; } = "dev.flowchat.identity.user.v1";
    public string RetryTopic { get; set; } = "dev.flowchat.identity.user.v1.retry";
    public string DeadLetterTopic { get; set; } = "dev.flowchat.identity.user.v1.dlq";
    public int MaxRetryCount { get; set; } = 5;
    public int RetryBaseDelaySeconds { get; set; } = 5;
    public int RetryMaxDelaySeconds { get; set; } = 300;
    public string AutoOffsetReset { get; set; } = "Earliest";
}
