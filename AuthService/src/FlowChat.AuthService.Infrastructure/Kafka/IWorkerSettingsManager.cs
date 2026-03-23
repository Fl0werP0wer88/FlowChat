namespace FlowChat.AuthService.Infrastructure.Kafka;

public interface IWorkerSettingsManager
{
    UserCreatedProducerOptions GetUserCreatedProducerOptions();

    UserEmailVerificationRequestedProducerOptions GetUserEmailVerificationRequestedProducerOptions();
}
