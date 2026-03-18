using Microsoft.Extensions.Configuration;

namespace FlowChat.RealtimeService.Infrastructure.Configuration;

public sealed class ApiSettingsManager(IConfiguration configuration) : IApiSettingsManager
{
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

    public JwtSettings GetJwtSettings() =>
        _configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();

    public ApiRuntimeSettings GetApiRuntimeSettings()
    {
        var settings = new ApiRuntimeSettings();

        _configuration.GetSection("FlowChat").Bind(settings);
        settings.ApiUrl = _configuration["ApiUrl"] ?? settings.ApiUrl;
        settings.BlazorUrl = _configuration["BlazorUrl"] ?? settings.BlazorUrl;

        return settings;
    }

    public InternalApiSettings GetInternalApiSettings() =>
        _configuration.GetSection(InternalApiSettings.SectionName).Get<InternalApiSettings>() ?? new InternalApiSettings();

    public RealtimeApiSettings GetRealtimeApiSettings() =>
        _configuration.GetSection(RealtimeApiSettings.SectionName).Get<RealtimeApiSettings>() ?? new RealtimeApiSettings();
}
