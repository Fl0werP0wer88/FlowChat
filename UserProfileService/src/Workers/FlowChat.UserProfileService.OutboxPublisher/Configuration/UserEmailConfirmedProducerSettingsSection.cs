namespace FlowChat.UserProfileService.OutboxPublisher.Configuration;

public sealed class UserEmailConfirmedProducerSettingsSection
{
    public const string SectionName = "Kafka:UserEmailConfirmedProducer";

    public string BootstrapServers { get; set; } = "localhost:9092";

    public string Topic { get; set; } = "dev.flowchat.user-profile.user-profile.v1";
}
