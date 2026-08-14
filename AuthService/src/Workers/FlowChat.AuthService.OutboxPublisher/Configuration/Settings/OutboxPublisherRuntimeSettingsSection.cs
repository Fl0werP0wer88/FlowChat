using FlowChat.Core.Contracts;

namespace FlowChat.AuthService.OutboxPublisher.Configuration.Settings;

public sealed class OutboxPublisherRuntimeSettingsSection : SettingsSectionBase
{
    public override string SectionName => "OutboxPublisher";

    public int BatchSize { get; set; } = 500;
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromMilliseconds(500);
    public TimeSpan InitialRetryDelay { get; set; } = TimeSpan.FromSeconds(3);
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(120);
}
