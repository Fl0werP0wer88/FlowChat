using FlowChat.Core.Contracts;

namespace FlowChat.PresenceService.Consumers.Configuration.Settings;

public sealed class PresenceApiSettingsSection : SettingsSectionBase
{
    public override string SectionName => "PresenceApi";

    public string BaseUrl { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;
}
