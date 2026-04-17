using Microsoft.Extensions.Configuration;

namespace FlowChat.AuthService.Infrastructure.Kafka;

public sealed class KafkaSettingsManager(IConfiguration configuration) : IKafkaSettingsManager
{
    private readonly IConfiguration _configuration = configuration;

    public AccountRegisteredProducerSettingsSection GetAccountRegisteredProducerSettingsSection() =>
        _configuration.GetSection(new AccountRegisteredProducerSettingsSection().SectionName).Get<AccountRegisteredProducerSettingsSection>()
        ?? new AccountRegisteredProducerSettingsSection();

    public AccountConfirmedProducerSettingsSection GetAccountConfirmedProducerSettingsSection() =>
        _configuration.GetSection(new AccountConfirmedProducerSettingsSection().SectionName).Get<AccountConfirmedProducerSettingsSection>()
        ?? new AccountConfirmedProducerSettingsSection();
}
