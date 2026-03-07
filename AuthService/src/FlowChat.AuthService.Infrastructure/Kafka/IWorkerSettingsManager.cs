using FlowChat.AuthService.Persistence.Configuration;

namespace FlowChat.AuthService.Infrastructure.Kafka;

public interface IWorkerSettingsManager
{
    UserCreatedProducerOptions GetUserCreatedProducerOptions();

    UserEmailVerificationRequestedProducerOptions GetUserEmailVerificationRequestedProducerOptions();

    OutboxPublisherRuntimeOptions GetOutboxPublisherRuntimeOptions();
}
