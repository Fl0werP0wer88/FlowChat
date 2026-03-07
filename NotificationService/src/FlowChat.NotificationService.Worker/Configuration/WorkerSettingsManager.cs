using FlowChat.NotificationService.Worker.Kafka;
using Microsoft.Extensions.Configuration;

namespace FlowChat.NotificationService.Worker.Configuration;

public sealed class WorkerSettingsManager : IWorkerSettingsManager
{
    private readonly IConfiguration _configuration;

    public WorkerSettingsManager(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public UserEmailVerificationRequestedConsumerOptions GetUserEmailVerificationRequestedConsumerOptions() =>
        _configuration.GetSection(UserEmailVerificationRequestedConsumerOptions.SectionName)
            .Get<UserEmailVerificationRequestedConsumerOptions>()
        ?? new UserEmailVerificationRequestedConsumerOptions();
}
