using Microsoft.Extensions.Configuration;

namespace FlowChat.AuthService.Infrastructure.Configuration;

public sealed class ApiSettingsManager : IApiSettingsManager
{
    private readonly IConfiguration _configuration;

    public ApiSettingsManager(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public JwtSettings GetJwtSettings() => ResolveSection<JwtSettings>(JwtSettings.SectionName);

    public ConfirmationLinksSettings GetConfirmationLinksSettings() =>
        ResolveSection<ConfirmationLinksSettings>(ConfirmationLinksSettings.SectionName);

    public ApiRuntimeSettings GetApiRuntimeSettings()
    {
        var settings = new ApiRuntimeSettings();

        _configuration.GetSection(ApiRuntimeSettings.FlowChatSectionName).Bind(settings);
        settings.ApiUrl = _configuration["ApiUrl"] ?? settings.ApiUrl;
        settings.BlazorUrl = _configuration["BlazorUrl"] ?? settings.BlazorUrl;
        settings.AuthDbConnectionString = _configuration.GetConnectionString("AuthDb") ?? string.Empty;

        return settings;
    }

    private TSettings ResolveSection<TSettings>(string sectionName)
        where TSettings : new()
    {
        return _configuration.GetSection(sectionName).Get<TSettings>() ?? new TSettings();
    }
}
