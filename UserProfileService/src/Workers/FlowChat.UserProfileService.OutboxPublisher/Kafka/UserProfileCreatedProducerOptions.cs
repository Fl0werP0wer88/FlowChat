namespace FlowChat.UserProfileService.OutboxPublisher.Kafka;

public sealed class UserProfileCreatedProducerOptions
{
    public const string SectionName = "Kafka:UserProfileCreatedProducer";

    public string BootstrapServers { get; set; } = "localhost:9092";

    public string Topic { get; set; } = "dev.flowchat.user-profile.user-profile.v1";
}
