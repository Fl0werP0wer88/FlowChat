using FlowChat.Core.Contracts;

namespace FlowChat.PresenceService.Infrastructure.Configuration.Settings;

public sealed class JwtSettingsSection : SettingsSectionBase
{
    public override string SectionName => "JwtSettings";

    public string Key { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;
}
