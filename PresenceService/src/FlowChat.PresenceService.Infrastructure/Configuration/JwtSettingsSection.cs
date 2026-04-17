namespace FlowChat.PresenceService.Infrastructure.Configuration;

public sealed class JwtSettingsSection
{
    public const string SectionName = "JwtSettings";

    public string Key { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;
}
