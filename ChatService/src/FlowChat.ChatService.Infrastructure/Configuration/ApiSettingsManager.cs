using Microsoft.Extensions.Configuration;

namespace FlowChat.ChatService.Infrastructure.Configuration;

public sealed class ApiSettingsManager : IApiSettingsManager
{
    private readonly IConfiguration _configuration;

    public ApiSettingsManager(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public ApiRuntimeSettings GetApiRuntimeSettings()
    {
        return new ApiRuntimeSettings
        {
            ApiUrl = _configuration["ApiUrl"] ?? "https://localhost:5000",
            BlazorUrl = _configuration["BlazorUrl"] ?? "https://localhost:5010"
        };
    }
}
