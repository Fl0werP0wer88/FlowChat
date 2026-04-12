using Microsoft.Extensions.Configuration;

namespace FlowChat.PresenceService.Infrastructure.Configuration;

public sealed class ApiSettingsManager(IConfiguration configuration) : IApiSettingsManager
{
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

    public JwtSettings GetJwtSettings() =>
        _configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();

    public InternalApiSettings GetInternalApiSettings() =>
        _configuration.GetSection(InternalApiSettings.SectionName).Get<InternalApiSettings>() ?? new InternalApiSettings();

    public PresenceStatusSettings GetPresenceStatusSettings()
    {
        var settings = _configuration.GetSection(PresenceStatusSettings.SectionName).Get<PresenceStatusSettings>()
            ?? new PresenceStatusSettings();

        settings.RedisConnectionString = _configuration.GetConnectionString(PresenceStatusSettings.RedisConnectionStringName)
            ?? settings.RedisConnectionString;

        return settings;
    }
}
