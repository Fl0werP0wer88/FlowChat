namespace FlowChat.RealtimeService.Infrastructure.Kafka;

public interface IKafkaSettingsManager
{
    RealtimeConnectionProducerOptions GetRealtimeConnectionProducerOptions();
}
