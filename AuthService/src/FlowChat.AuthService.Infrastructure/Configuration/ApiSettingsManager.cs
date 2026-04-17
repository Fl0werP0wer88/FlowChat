using Microsoft.Extensions.Configuration;

namespace FlowChat.AuthService.Infrastructure.Configuration;

public sealed class ApiSettingsManager : IApiSettingsManager
{
    private readonly IConfiguration _configuration;

    public ApiSettingsManager(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public JwtSettingsSection GetJwtSettingsSection() => ResolveSection<JwtSettingsSection>(new JwtSettingsSection().SectionName);

    public InternalApiSettingsSection GetInternalApiSettingsSection() =>
        ResolveSection<InternalApiSettingsSection>(new InternalApiSettingsSection().SectionName);

    public ApiRuntimeSettingsSection GetApiRuntimeSettingsSection()
    {
        var settings = new ApiRuntimeSettingsSection();

        _configuration.GetSection(new ApiRuntimeSettingsSection().SectionName).Bind(settings);
        settings.ApiUrl = _configuration["ApiUrl"] ?? settings.ApiUrl;
        settings.BlazorUrl = _configuration["BlazorUrl"] ?? settings.BlazorUrl;

        return settings;
    }

    private TSettings ResolveSection<TSettings>(string sectionName)
        where TSettings : new()
    {
        return _configuration.GetSection(sectionName).Get<TSettings>() ?? new TSettings();
    }
}
