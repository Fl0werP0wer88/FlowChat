using FlowChat.Core.Contracts;

namespace FlowChat.GatewayService.Api.Configuration.Settings;

public sealed class GatewayCatalogSettingsSection : SettingsSectionBase
{
    public override string SectionName => "GatewayCatalog";

    public List<GatewayRouteCatalogEntry> Routes { get; set; } = [];
}

public sealed class GatewayRouteCatalogEntry
{
    public string Name { get; set; } = string.Empty;

    public string PublicPath { get; set; } = string.Empty;

    public string ClusterId { get; set; } = string.Empty;

    public bool RequiresAuthentication { get; set; }
}
