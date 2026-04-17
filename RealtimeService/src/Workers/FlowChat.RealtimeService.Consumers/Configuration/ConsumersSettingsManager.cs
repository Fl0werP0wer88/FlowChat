using FlowChat.RealtimeService.Consumers.Kafka;
using FlowChat.RealtimeService.Routing.Configuration;
using Microsoft.Extensions.Configuration;

namespace FlowChat.RealtimeService.Consumers.Configuration;

public sealed class ConsumersSettingsManager(IConfiguration configuration) : IConsumersSettingsManager
{
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

    public ChatMessageSentConsumerSettingsSection GetChatMessageSentConsumerSettingsSection() =>
        _configuration.GetSection(new ChatMessageSentConsumerSettingsSection().SectionName).Get<ChatMessageSentConsumerSettingsSection>()
        ?? new ChatMessageSentConsumerSettingsSection();

    public PresenceStatusChangedConsumerSettingsSection GetPresenceStatusChangedConsumerSettingsSection() =>
        _configuration.GetSection(new PresenceStatusChangedConsumerSettingsSection().SectionName).Get<PresenceStatusChangedConsumerSettingsSection>()
        ?? new PresenceStatusChangedConsumerSettingsSection();

    public RealtimeApiSettingsSection GetRealtimeApiSettingsSection() =>
        _configuration.GetSection(new RealtimeApiSettingsSection().SectionName).Get<RealtimeApiSettingsSection>()
        ?? new RealtimeApiSettingsSection();

    public RealtimeRoutingSettingsSection GetRealtimeRoutingSettingsSection() =>
        _configuration.GetSection(new RealtimeRoutingSettingsSection().SectionName).Get<RealtimeRoutingSettingsSection>()
        ?? new RealtimeRoutingSettingsSection();
}
