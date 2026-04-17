using Microsoft.Extensions.Configuration;

namespace FlowChat.RealtimeService.Infrastructure.Configuration;

public sealed class ApiSettingsManager(IConfiguration configuration) : IApiSettingsManager
{
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

    public JwtSettingsSection GetJwtSettingsSection() =>
        _configuration.GetSection(new JwtSettingsSection().SectionName).Get<JwtSettingsSection>() ?? new JwtSettingsSection();

    public ApiRuntimeSettingsSection GetApiRuntimeSettingsSection()
    {
        var settings = new ApiRuntimeSettingsSection();

        _configuration.GetSection(new ApiRuntimeSettingsSection().SectionName).Bind(settings);
        settings.ApiUrl = _configuration["ApiUrl"] ?? settings.ApiUrl;
        settings.BlazorUrl = _configuration["BlazorUrl"] ?? settings.BlazorUrl;

        return settings;
    }

    public InternalApiSettingsSection GetInternalApiSettingsSection() =>
        _configuration.GetSection(new InternalApiSettingsSection().SectionName).Get<InternalApiSettingsSection>() ?? new InternalApiSettingsSection();

    public RealtimeConnectionsSettingsSection GetRealtimeConnectionsSettingsSection()
    {
        var settings = _configuration.GetSection(new RealtimeConnectionsSettingsSection().SectionName).Get<RealtimeConnectionsSettingsSection>()
            ?? new RealtimeConnectionsSettingsSection();

        settings.RedisConnectionString = _configuration.GetConnectionString(RealtimeConnectionsSettingsSection.RedisConnectionStringName)
            ?? settings.RedisConnectionString;

        return settings;
    }

    public PresenceServiceSettingsSection GetPresenceServiceSettingsSection() =>
        _configuration.GetSection(new PresenceServiceSettingsSection().SectionName).Get<PresenceServiceSettingsSection>()
            ?? new PresenceServiceSettingsSection();
}
