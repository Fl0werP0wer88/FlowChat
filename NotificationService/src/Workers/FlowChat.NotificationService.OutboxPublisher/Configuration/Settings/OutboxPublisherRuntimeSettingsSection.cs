using FlowChat.Core.Contracts;

namespace FlowChat.NotificationService.OutboxPublisher.Configuration.Settings;

public sealed class OutboxPublisherRuntimeSettingsSection : SettingsSectionBase
{
    public override string SectionName => "OutboxPublisher";

    public int BatchSize { get; set; } = 500;
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromMilliseconds(500);
    public int RetryBaseDelaySeconds { get; set; } = 3;
    public int MaxRetryDelaySeconds { get; set; } = 120;
}
