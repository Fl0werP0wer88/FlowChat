using FlowChat.UserProfileService.Worker.Kafka;

namespace FlowChat.UserProfileService.Worker.Configuration;

public interface IWorkerSettingsManager
{
    UserCreatedConsumerOptions GetUserCreatedConsumerOptions();
}
