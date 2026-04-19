using FlowChat.Core.Contracts;

namespace FlowChat.SocialGraphService.Infrastructure.Configuration.Settings;

public sealed class ApiRuntimeSettingsSection : SettingsSectionBase
{
    public override string SectionName => "FlowChat";

    public string ApiUrl { get; set; } = "https://localhost:5000";
}
