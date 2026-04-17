using Microsoft.Extensions.Configuration;

namespace FlowChat.PresenceService.Infrastructure.Configuration;

public sealed class ApiSettingsManager(IConfiguration configuration) : IApiSettingsManager
{
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

    public JwtSettingsSection GetJwtSettingsSection() =>
        _configuration.GetSection(JwtSettingsSection.SectionName).Get<JwtSettingsSection>() ?? new JwtSettingsSection();

    public InternalApiSettingsSection GetInternalApiSettingsSection() =>
        _configuration.GetSection(InternalApiSettingsSection.SectionName).Get<InternalApiSettingsSection>() ?? new InternalApiSettingsSection();

    public PresenceStatusSettingsSection GetPresenceStatusSettingsSection()
    {
        var settings = _configuration.GetSection(PresenceStatusSettingsSection.SectionName).Get<PresenceStatusSettingsSection>()
            ?? new PresenceStatusSettingsSection();

        settings.RedisConnectionString = _configuration.GetConnectionString(PresenceStatusSettingsSection.RedisConnectionStringName)
            ?? settings.RedisConnectionString;

        return settings;
    }
}
