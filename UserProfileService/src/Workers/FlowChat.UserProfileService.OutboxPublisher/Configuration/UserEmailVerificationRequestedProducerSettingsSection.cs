namespace FlowChat.UserProfileService.OutboxPublisher.Configuration;

public sealed class UserEmailVerificationRequestedProducerSettingsSection
{
    public const string SectionName = "Kafka:UserEmailVerificationRequestedProducer";

    public string BootstrapServers { get; set; } = "localhost:9092";

    public string Topic { get; set; } = "dev.flowchat.notification.email.v1";
}
