namespace FlowChat.RealtimeService.Infrastructure.Kafka;

public interface IKafkaSettingsManager
{
    RealtimeConnectionProducerSettingsSection GetRealtimeConnectionProducerSettingsSection();
}
