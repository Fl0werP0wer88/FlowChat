namespace FlowChat.UserProfileService.Worker.Kafka;

public sealed class UserCreatedConsumerOptions
{
    public const string SectionName = "Kafka:UserCreatedConsumer";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string GroupId { get; set; } = "auth-service";
    public string Topic { get; set; } = "flowchat.identity.user.created.v1";
    public string AutoOffsetReset { get; set; } = "Earliest";
}
