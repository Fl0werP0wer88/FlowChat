using Microsoft.Extensions.Configuration;

namespace FlowChat.AuthService.Infrastructure.Kafka;

public sealed class WorkerSettingsManager : IWorkerSettingsManager
{
    private readonly IConfiguration _configuration;

    public WorkerSettingsManager(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public UserCreatedProducerOptions GetUserCreatedProducerOptions() =>
        ResolveSection<UserCreatedProducerOptions>(UserCreatedProducerOptions.SectionName);

    public UserEmailVerificationRequestedProducerOptions GetUserEmailVerificationRequestedProducerOptions() =>
        ResolveSection<UserEmailVerificationRequestedProducerOptions>(
            UserEmailVerificationRequestedProducerOptions.SectionName);

    private TOptions ResolveSection<TOptions>(string sectionName)
        where TOptions : new()
    {
        return _configuration.GetSection(sectionName).Get<TOptions>() ?? new TOptions();
    }
}
