namespace FlowChat.UserProfileService.Worker.Kafka;

public sealed class UserCreatedConsumerOptions
{
    public const string SectionName = "Kafka:UserCreatedConsumer";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string GroupId { get; set; } = "userprofile-service";
    public string Topic { get; set; } = "flowchat.identity.user.created.v1";
    public string RetryTopic { get; set; } = "flowchat.identity.user.created.v1.retry";
    public string DeadLetterTopic { get; set; } = "flowchat.identity.user.created.v1.dlt";
    public int MaxRetryCount { get; set; } = 5;
    public string AutoOffsetReset { get; set; } = "Earliest";
}
