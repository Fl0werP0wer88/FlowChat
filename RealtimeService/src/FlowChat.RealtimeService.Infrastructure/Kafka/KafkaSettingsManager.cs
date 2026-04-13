using Microsoft.Extensions.Configuration;

namespace FlowChat.RealtimeService.Infrastructure.Kafka;

public sealed class KafkaSettingsManager(IConfiguration configuration) : IKafkaSettingsManager
{
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

    public RealtimeConnectionProducerOptions GetRealtimeConnectionProducerOptions() =>
        _configuration.GetSection(RealtimeConnectionProducerOptions.SectionName).Get<RealtimeConnectionProducerOptions>()
        ?? new RealtimeConnectionProducerOptions();
}
