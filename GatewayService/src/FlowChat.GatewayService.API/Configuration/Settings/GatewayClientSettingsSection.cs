using FlowChat.Core.Contracts;

namespace FlowChat.GatewayService.Api.Configuration.Settings;

public sealed class GatewayClientSettingsSection : SettingsSectionBase
{
    public override string SectionName => "GatewayClient";

    public List<string> AllowedOrigins { get; set; } = [];
}
