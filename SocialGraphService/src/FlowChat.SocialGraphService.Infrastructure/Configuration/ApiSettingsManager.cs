using Microsoft.Extensions.Configuration;

namespace FlowChat.SocialGraphService.Infrastructure.Configuration;

public sealed class ApiSettingsManager : IApiSettingsManager
{
    private readonly IConfiguration _configuration;

    public ApiSettingsManager(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public JwtSettingsSection GetJwtSettingsSection() =>
        _configuration.GetSection(new JwtSettingsSection().SectionName).Get<JwtSettingsSection>() ?? new JwtSettingsSection();

    public ApiRuntimeSettingsSection GetApiRuntimeSettingsSection()
    {
        var settings = new ApiRuntimeSettingsSection();

        _configuration.GetSection(new ApiRuntimeSettingsSection().SectionName).Bind(settings);
        settings.ApiUrl = _configuration["ApiUrl"] ?? settings.ApiUrl;

        return settings;
    }

    public InternalApiSettingsSection GetInternalApiSettingsSection() =>
        _configuration.GetSection(new InternalApiSettingsSection().SectionName).Get<InternalApiSettingsSection>()
        ?? new InternalApiSettingsSection();
}
