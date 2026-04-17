using FlowChat.Core.Contracts;

namespace FlowChat.AuthService.Infrastructure.Configuration;

public sealed class JwtSettingsSection : SettingsSectionBase
{
    public override string SectionName => "JwtSettings";

    public string Key { get; set; } = string.Empty;

    public string EncryptionKey { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public int ExpiresMinutes { get; set; } = 60;

    public int RefreshTokenExpiresMinutes { get; set; } = 10080; // 7 days
}
