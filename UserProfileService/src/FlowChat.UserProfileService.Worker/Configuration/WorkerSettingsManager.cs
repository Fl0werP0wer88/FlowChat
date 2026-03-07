using FlowChat.UserProfileService.Worker.Kafka;
using Microsoft.Extensions.Configuration;

namespace FlowChat.UserProfileService.Worker.Configuration;

public sealed class WorkerSettingsManager : IWorkerSettingsManager
{
    private readonly IConfiguration _configuration;

    public WorkerSettingsManager(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public UserCreatedConsumerOptions GetUserCreatedConsumerOptions() =>
        _configuration.GetSection(UserCreatedConsumerOptions.SectionName).Get<UserCreatedConsumerOptions>()
        ?? new UserCreatedConsumerOptions();
}
