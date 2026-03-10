namespace FlowChat.UserProfileService.Infrastructure.Kafka;

public interface IKafkaSettingsManager
{
    UserProfileCreatedProducerOptions GetUserProfileCreatedProducerOptions();
}
