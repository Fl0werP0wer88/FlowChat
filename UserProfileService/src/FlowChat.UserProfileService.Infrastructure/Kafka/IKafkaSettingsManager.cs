namespace FlowChat.UserProfileService.Infrastructure.Kafka;

public interface IKafkaSettingsManager
{
    UserProfileCreatedProducerOptions GetUserProfileCreatedProducerOptions();
    UserEmailConfirmedProducerOptions GetUserEmailConfirmedProducerOptions();
    UserEmailVerificationRequestedProducerOptions GetUserEmailVerificationRequestedProducerOptions();
    UserProfileStateChangedProducerOptions GetUserProfileStateChangedProducerOptions();
}
