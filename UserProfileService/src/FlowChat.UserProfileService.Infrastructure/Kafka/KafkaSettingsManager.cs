using Microsoft.Extensions.Configuration;

namespace FlowChat.UserProfileService.Infrastructure.Kafka;

public sealed class KafkaSettingsManager(IConfiguration configuration) : IKafkaSettingsManager
{
    private readonly IConfiguration _configuration = configuration;

    public UserProfileCreatedProducerOptions GetUserProfileCreatedProducerOptions() =>
        _configuration.GetSection(UserProfileCreatedProducerOptions.SectionName).Get<UserProfileCreatedProducerOptions>()
        ?? new UserProfileCreatedProducerOptions();

    public UserEmailConfirmedProducerOptions GetUserEmailConfirmedProducerOptions() =>
        _configuration.GetSection(UserEmailConfirmedProducerOptions.SectionName).Get<UserEmailConfirmedProducerOptions>()
        ?? new UserEmailConfirmedProducerOptions();

    public UserProfileStateChangedProducerOptions GetUserProfileStateChangedProducerOptions() =>
        _configuration.GetSection(UserProfileStateChangedProducerOptions.SectionName).Get<UserProfileStateChangedProducerOptions>()
        ?? new UserProfileStateChangedProducerOptions();
}
