namespace FlowChat.PresenceService.Infrastructure.Kafka;

public interface IKafkaSettingsManager
{
    PresenceStatusChangedProducerSettingsSection GetPresenceStatusChangedProducerSettingsSection();
}
