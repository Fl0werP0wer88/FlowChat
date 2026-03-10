using Microsoft.Extensions.Configuration;

namespace FlowChat.UserProfileService.Infrastructure.Kafka;

public sealed class KafkaSettingsManager(IConfiguration configuration) : IKafkaSettingsManager
{
    private readonly IConfiguration _configuration = configuration;

    public UserProfileCreatedProducerOptions GetUserProfileCreatedProducerOptions() =>
        _configuration.GetSection(UserProfileCreatedProducerOptions.SectionName).Get<UserProfileCreatedProducerOptions>()
        ?? new UserProfileCreatedProducerOptions();
}
