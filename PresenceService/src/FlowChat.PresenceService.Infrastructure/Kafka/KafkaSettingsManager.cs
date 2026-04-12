using Microsoft.Extensions.Configuration;

namespace FlowChat.PresenceService.Infrastructure.Kafka;

public sealed class KafkaSettingsManager(IConfiguration configuration) : IKafkaSettingsManager
{
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

    public UserStatusChangedProducerOptions GetUserStatusChangedProducerOptions() =>
        _configuration.GetSection(UserStatusChangedProducerOptions.SectionName).Get<UserStatusChangedProducerOptions>()
        ?? new UserStatusChangedProducerOptions();
}
