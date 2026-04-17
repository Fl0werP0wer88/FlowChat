namespace FlowChat.GatewayService.Api.Configuration;

public sealed class JwtSettingsSection
{
    public const string SectionName = "JwtSettingsSection";

    public string Key { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;
}
