using Microsoft.Extensions.Configuration;

namespace FlowChat.UserProfileService.Infrastructure.Configuration;

public sealed class ApiSettingsManager : IApiSettingsManager
{
    private readonly IConfiguration _configuration;

    public ApiSettingsManager(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public ApiRuntimeSettings GetApiRuntimeSettings()
    {
        var settings = new ApiRuntimeSettings();

        _configuration.GetSection("FlowChat").Bind(settings);
        settings.ApiUrl = _configuration["ApiUrl"] ?? settings.ApiUrl;
        settings.BlazorUrl = _configuration["BlazorUrl"] ?? settings.BlazorUrl;

        return settings;
    }

    public InternalApiSettings GetInternalApiSettings() =>
        _configuration.GetSection(InternalApiSettings.SectionName).Get<InternalApiSettings>()
        ?? new InternalApiSettings();
}
