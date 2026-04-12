namespace FlowChat.PresenceService.Infrastructure.Kafka;

public interface IKafkaSettingsManager
{
    PresenceStatusChangedProducerOptions GetPresenceStatusChangedProducerOptions();
}
