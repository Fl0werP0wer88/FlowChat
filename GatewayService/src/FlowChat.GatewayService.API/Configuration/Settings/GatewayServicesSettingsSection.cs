using FlowChat.Core.Contracts;

namespace FlowChat.GatewayService.Api.Configuration.Settings;

public sealed class GatewayServicesSettingsSection : SettingsSectionBase
{
    public override string SectionName => "GatewayServices";

    public string SocialGraphServiceBaseUrl { get; set; } = string.Empty;

    public string ChatServiceBaseUrl { get; set; } = string.Empty;
}
