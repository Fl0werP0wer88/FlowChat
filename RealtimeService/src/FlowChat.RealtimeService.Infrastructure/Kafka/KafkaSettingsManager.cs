using Microsoft.Extensions.Configuration;

namespace FlowChat.RealtimeService.Infrastructure.Kafka;

public sealed class KafkaSettingsManager(IConfiguration configuration) : IKafkaSettingsManager
{
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

    public RealtimeConnectionProducerSettingsSection GetRealtimeConnectionProducerSettingsSection() =>
        _configuration.GetSection(RealtimeConnectionProducerSettingsSection.SectionName).Get<RealtimeConnectionProducerSettingsSection>()
        ?? new RealtimeConnectionProducerSettingsSection();
}
