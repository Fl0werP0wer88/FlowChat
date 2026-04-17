using Microsoft.Extensions.Configuration;

namespace FlowChat.UserProfileService.Infrastructure.Configuration;

public sealed class ApiSettingsManager : IApiSettingsManager
{
    private readonly IConfiguration _configuration;

    public ApiSettingsManager(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public ApiRuntimeSettingsSection GetApiRuntimeSettingsSection()
    {
        var settings = new ApiRuntimeSettingsSection();

        _configuration.GetSection(ApiRuntimeSettingsSection.SectionName).Bind(settings);
        settings.ApiUrl = _configuration["ApiUrl"] ?? settings.ApiUrl;
        settings.BlazorUrl = _configuration["BlazorUrl"] ?? settings.BlazorUrl;

        return settings;
    }

    public ConfirmationLinksSettingsSection GetConfirmationLinksSettingsSection() =>
        _configuration.GetSection(ConfirmationLinksSettingsSection.SectionName).Get<ConfirmationLinksSettingsSection>()
        ?? new ConfirmationLinksSettingsSection();

    public InternalApiSettingsSection GetInternalApiSettingsSection() =>
        _configuration.GetSection(InternalApiSettingsSection.SectionName).Get<InternalApiSettingsSection>()
        ?? new InternalApiSettingsSection();
}
