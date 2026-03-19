using Microsoft.Extensions.Configuration;

namespace FlowChat.ChatService.Infrastructure.Configuration;

public sealed class ApiSettingsManager : IApiSettingsManager
{
    private readonly IConfiguration _configuration;

    public ApiSettingsManager(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public ApiRuntimeSettings GetApiRuntimeSettings()
    {
        var settings = new ApiRuntimeSettings();

        _configuration.GetSection("FlowChat").Bind(settings);
        settings.ApiUrl = _configuration["ApiUrl"] ?? settings.ApiUrl;
        settings.BlazorUrl = _configuration["BlazorUrl"] ?? settings.BlazorUrl;

        return settings;
    }
}
