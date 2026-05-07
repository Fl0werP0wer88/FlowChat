using FlowChat.Core.Contracts;

namespace FlowChat.RealtimeService.Infrastructure.Configuration.Settings;

public sealed class PresenceServiceSettingsSection : SettingsSectionBase, IInternalApiSettingSection
{
    public override string SectionName => "PresenceService";

    public string BaseUrl { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;
}
