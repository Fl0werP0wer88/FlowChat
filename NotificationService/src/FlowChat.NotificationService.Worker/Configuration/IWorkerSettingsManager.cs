using FlowChat.NotificationService.Worker.Kafka;

namespace FlowChat.NotificationService.Worker.Configuration;

public interface IWorkerSettingsManager
{
    UserCreatedConsumerOptions GetUserCreatedConsumerOptions();
}
