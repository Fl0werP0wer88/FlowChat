using FlowChat.Core.Contracts;

namespace FlowChat.PresenceService.OutboxPublisher.Configuration.Settings;

public sealed class OutboxPublisherRuntimeSettingsSection : SettingsSectionBase
{
    public override string SectionName => "OutboxPublisher";

    public int BatchSize { get; set; } = 25;

    public int PollIntervalSeconds { get; set; } = 3;

    public int RetryBaseDelaySeconds { get; set; } = 3;

    public int MaxRetryDelaySeconds { get; set; } = 120;
}
