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
        _configuration.GetSection(JwtSettingsSection.SectionName).Get<JwtSettingsSection>() ?? new JwtSettingsSection();

    public ApiRuntimeSettingsSection GetApiRuntimeSettingsSection()
    {
        var settings = new ApiRuntimeSettingsSection();

        _configuration.GetSection(ApiRuntimeSettingsSection.SectionName).Bind(settings);
        settings.ApiUrl = _configuration["ApiUrl"] ?? settings.ApiUrl;
        settings.BlazorUrl = _configuration["BlazorUrl"] ?? settings.BlazorUrl;

        return settings;
    }

    public InternalApiSettingsSection GetInternalApiSettingsSection() =>
        _configuration.GetSection(InternalApiSettingsSection.SectionName).Get<InternalApiSettingsSection>()
        ?? new InternalApiSettingsSection();
}
