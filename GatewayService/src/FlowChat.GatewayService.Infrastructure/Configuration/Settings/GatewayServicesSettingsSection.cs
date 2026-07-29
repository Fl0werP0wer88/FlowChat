using FlowChat.Core.Contracts;

namespace FlowChat.GatewayService.Infrastructure.Configuration.Settings;

public sealed class GatewayServicesSettingsSection : SettingsSectionBase
{
    public override string SectionName => "GatewayServices";

    public string ChatServiceBaseUrl { get; set; } = string.Empty;

    public string ChatServiceInternalApiKey { get; set; } = string.Empty;

    public string PresenceServiceBaseUrl { get; set; } = string.Empty;

    public string PresenceServiceInternalApiKey { get; set; } = string.Empty;
}
