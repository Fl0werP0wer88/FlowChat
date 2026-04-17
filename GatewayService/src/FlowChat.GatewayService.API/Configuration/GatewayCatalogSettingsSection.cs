namespace FlowChat.GatewayService.Api.Configuration;

public sealed class GatewayCatalogSettingsSection
{
    public const string SectionName = "GatewayCatalog";

    public List<GatewayRouteCatalogEntry> Routes { get; set; } = [];
}

public sealed class GatewayRouteCatalogEntry
{
    public string Name { get; set; } = string.Empty;

    public string PublicPath { get; set; } = string.Empty;

    public string ClusterId { get; set; } = string.Empty;

    public bool RequiresAuthentication { get; set; }
}
