namespace FlowChat.GatewayService.Api.Configuration;

public sealed class GatewayClientSettings
{
    public const string SectionName = "GatewayClient";

    public List<string> AllowedOrigins { get; set; } = [];
}
