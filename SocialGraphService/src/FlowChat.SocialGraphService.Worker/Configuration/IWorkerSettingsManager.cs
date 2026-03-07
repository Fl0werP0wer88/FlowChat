using FlowChat.SocialGraphService.Worker.Kafka;

namespace FlowChat.SocialGraphService.Worker.Configuration;

public interface IWorkerSettingsManager
{
    UserCreatedConsumerOptions GetUserCreatedConsumerOptions();
}
