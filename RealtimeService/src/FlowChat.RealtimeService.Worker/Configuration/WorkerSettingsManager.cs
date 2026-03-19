using FlowChat.RealtimeService.Worker.Kafka;
using Microsoft.Extensions.Configuration;

namespace FlowChat.RealtimeService.Worker.Configuration;

public sealed class WorkerSettingsManager(IConfiguration configuration) : IWorkerSettingsManager
{
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

    public ChatMessageSentConsumerOptions GetChatMessageSentConsumerOptions() =>
        _configuration.GetSection(ChatMessageSentConsumerOptions.SectionName).Get<ChatMessageSentConsumerOptions>()
        ?? new ChatMessageSentConsumerOptions();

    public UserPresenceChangedConsumerOptions GetUserPresenceChangedConsumerOptions() =>
        _configuration.GetSection(UserPresenceChangedConsumerOptions.SectionName).Get<UserPresenceChangedConsumerOptions>()
        ?? new UserPresenceChangedConsumerOptions();

    public RealtimeApiSettings GetRealtimeApiSettings() =>
        _configuration.GetSection(RealtimeApiSettings.SectionName).Get<RealtimeApiSettings>()
        ?? new RealtimeApiSettings();
}
