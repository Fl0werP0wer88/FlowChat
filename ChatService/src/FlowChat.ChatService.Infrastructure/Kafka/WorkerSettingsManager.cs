using FlowChat.ChatService.Persistence.Configuration;
using Microsoft.Extensions.Configuration;

namespace FlowChat.ChatService.Infrastructure.Kafka;

public sealed class WorkerSettingsManager : IWorkerSettingsManager
{
    private readonly IConfiguration _configuration;

    public WorkerSettingsManager(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public ChatMessageSentProducerSettingsSection GetChatMessageSentProducerSettingsSection() =>
        ResolveSection<ChatMessageSentProducerSettingsSection>(new ChatMessageSentProducerSettingsSection().SectionName);

    public OutboxPublisherRuntimeSettingsSection GetOutboxPublisherRuntimeSettingsSection() =>
        ResolveSection<OutboxPublisherRuntimeSettingsSection>(new OutboxPublisherRuntimeSettingsSection().SectionName);

    private TOptions ResolveSection<TOptions>(string sectionName)
        where TOptions : new() =>
        _configuration.GetSection(sectionName).Get<TOptions>() ?? new TOptions();
}
