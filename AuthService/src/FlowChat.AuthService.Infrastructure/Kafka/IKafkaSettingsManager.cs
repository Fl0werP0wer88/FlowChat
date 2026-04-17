namespace FlowChat.AuthService.Infrastructure.Kafka;

public interface IKafkaSettingsManager
{
    AccountRegisteredProducerSettingsSection GetAccountRegisteredProducerSettingsSection();
    AccountConfirmedProducerSettingsSection GetAccountConfirmedProducerSettingsSection();
}
