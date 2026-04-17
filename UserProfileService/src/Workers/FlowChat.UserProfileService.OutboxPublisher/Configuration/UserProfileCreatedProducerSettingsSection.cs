namespace FlowChat.UserProfileService.OutboxPublisher.Configuration;

public sealed class UserProfileCreatedProducerSettingsSection
{
    public const string SectionName = "Kafka:UserProfileCreatedProducer";

    public string BootstrapServers { get; set; } = "localhost:9092";

    public string Topic { get; set; } = "dev.flowchat.user-profile.user-profile.v1";
}
