using FlowChat.Core.Contracts;

namespace FlowChat.RealtimeService.Consumers.Configuration.Settings;

public sealed class RealtimeApiSettingsSection : SettingsSectionBase
{
    public override string SectionName => "RealtimeApi";

    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = string.Empty;
}
