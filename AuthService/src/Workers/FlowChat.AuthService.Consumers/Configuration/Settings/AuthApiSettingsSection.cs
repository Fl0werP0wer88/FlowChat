using FlowChat.Core.Contracts;

namespace FlowChat.AuthService.Consumers.Configuration.Settings;

public sealed class AuthApiSettingsSection : SettingsSectionBase
{
    public override string SectionName => "AuthApi";

    public string BaseUrl { get; set; } = "https://localhost:7236";

    public string ApiKey { get; set; } = string.Empty;
}
