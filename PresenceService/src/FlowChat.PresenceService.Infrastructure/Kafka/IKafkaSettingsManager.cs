namespace FlowChat.PresenceService.Infrastructure.Kafka;

public interface IKafkaSettingsManager
{
    UserStatusChangedProducerOptions GetUserStatusChangedProducerOptions();
}
