using Microsoft.Extensions.Configuration;

namespace FlowChat.RealtimeService.Infrastructure.Kafka;

public sealed class KafkaSettingsManager(IConfiguration configuration) : IKafkaSettingsManager
{
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

    public RealtimeConnectionRegisteredProducerSettingsSection GetRealtimeConnectionRegisteredProducerSettingsSection() =>
        _configuration.GetSection(new RealtimeConnectionRegisteredProducerSettingsSection().SectionName)
            .Get<RealtimeConnectionRegisteredProducerSettingsSection>()
        ?? new RealtimeConnectionRegisteredProducerSettingsSection();

    public RealtimeConnectionUnregisteredProducerSettingsSection GetRealtimeConnectionUnregisteredProducerSettingsSection() =>
        _configuration.GetSection(new RealtimeConnectionUnregisteredProducerSettingsSection().SectionName)
            .Get<RealtimeConnectionUnregisteredProducerSettingsSection>()
        ?? new RealtimeConnectionUnregisteredProducerSettingsSection();
}
