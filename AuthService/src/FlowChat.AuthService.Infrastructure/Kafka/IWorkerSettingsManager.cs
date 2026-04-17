namespace FlowChat.AuthService.Infrastructure.Kafka;

public interface IWorkerSettingsManager
{
    AccountRegisteredProducerSettingsSection GetAccountRegisteredProducerSettingsSection();
}
