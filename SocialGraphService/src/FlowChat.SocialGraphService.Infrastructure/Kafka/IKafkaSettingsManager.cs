namespace FlowChat.SocialGraphService.Infrastructure.Kafka;

public interface IKafkaSettingsManager
{
    ContactAddedProducerOptions GetContactAddedProducerOptions();
    ContactDeletedProducerOptions GetContactDeletedProducerOptions();
}
