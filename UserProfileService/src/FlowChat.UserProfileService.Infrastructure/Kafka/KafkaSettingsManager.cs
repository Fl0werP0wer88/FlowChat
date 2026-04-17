using Microsoft.Extensions.Configuration;

namespace FlowChat.UserProfileService.Infrastructure.Kafka;

public sealed class KafkaSettingsManager(IConfiguration configuration) : IKafkaSettingsManager
{
    private readonly IConfiguration _configuration = configuration;

    public UserProfileCreatedProducerSettingsSection GetUserProfileCreatedProducerSettingsSection() =>
        _configuration.GetSection(UserProfileCreatedProducerSettingsSection.SectionName).Get<UserProfileCreatedProducerSettingsSection>()
        ?? new UserProfileCreatedProducerSettingsSection();

    public UserEmailConfirmedProducerSettingsSection GetUserEmailConfirmedProducerSettingsSection() =>
        _configuration.GetSection(UserEmailConfirmedProducerSettingsSection.SectionName).Get<UserEmailConfirmedProducerSettingsSection>()
        ?? new UserEmailConfirmedProducerSettingsSection();

    public UserEmailVerificationRequestedProducerSettingsSection GetUserEmailVerificationRequestedProducerSettingsSection() =>
        _configuration.GetSection(UserEmailVerificationRequestedProducerSettingsSection.SectionName).Get<UserEmailVerificationRequestedProducerSettingsSection>()
        ?? new UserEmailVerificationRequestedProducerSettingsSection();

    public UserProfileStateChangedProducerSettingsSection GetUserProfileStateChangedProducerSettingsSection() =>
        _configuration.GetSection(UserProfileStateChangedProducerSettingsSection.SectionName).Get<UserProfileStateChangedProducerSettingsSection>()
        ?? new UserProfileStateChangedProducerSettingsSection();
}
