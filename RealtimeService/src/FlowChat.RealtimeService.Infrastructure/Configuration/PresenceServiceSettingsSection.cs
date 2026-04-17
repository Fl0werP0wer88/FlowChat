using FlowChat.Core.Contracts;

namespace FlowChat.RealtimeService.Infrastructure.Configuration;

public sealed class PresenceServiceSettingsSection : SettingsSectionBase
{
    public override string SectionName => "PresenceService";

    public string BaseUrl { get; set; } = string.Empty;

    public string InternalApiKey { get; set; } = string.Empty;
}
