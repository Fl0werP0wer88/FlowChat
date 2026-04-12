using Microsoft.Extensions.Configuration;

namespace FlowChat.PresenceService.Infrastructure.Kafka;

public sealed class KafkaSettingsManager(IConfiguration configuration) : IKafkaSettingsManager
{
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

    public PresenceStatusChangedProducerOptions GetPresenceStatusChangedProducerOptions() =>
        _configuration.GetSection(PresenceStatusChangedProducerOptions.SectionName).Get<PresenceStatusChangedProducerOptions>()
        ?? new PresenceStatusChangedProducerOptions();
}
