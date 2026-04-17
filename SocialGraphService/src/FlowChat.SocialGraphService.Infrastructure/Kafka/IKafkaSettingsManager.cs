namespace FlowChat.SocialGraphService.Infrastructure.Kafka;

public interface IKafkaSettingsManager
{
    ContactAddedProducerSettingsSection GetContactAddedProducerSettingsSection();
    ContactDeletedProducerSettingsSection GetContactDeletedProducerSettingsSection();
}
