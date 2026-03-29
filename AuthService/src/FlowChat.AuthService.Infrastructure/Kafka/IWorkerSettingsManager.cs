namespace FlowChat.AuthService.Infrastructure.Kafka;

public interface IWorkerSettingsManager
{
    AccountRegisteredProducerOptions GetAccountRegisteredProducerOptions();

    UserEmailVerificationRequestedProducerOptions GetUserEmailVerificationRequestedProducerOptions();
}
