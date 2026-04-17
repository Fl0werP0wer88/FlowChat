using Microsoft.Extensions.Configuration;

namespace FlowChat.UserProfileService.Infrastructure.Configuration;

public sealed class ApiSettingsManager : IApiSettingsManager
{
    private readonly IConfiguration _configuration;

    public ApiSettingsManager(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public ConfirmationLinksSettingsSection GetConfirmationLinksSettingsSection() =>
        _configuration.GetSection(new ConfirmationLinksSettingsSection().SectionName).Get<ConfirmationLinksSettingsSection>()
        ?? new ConfirmationLinksSettingsSection();

    public InternalApiSettingsSection GetInternalApiSettingsSection() =>
        _configuration.GetSection(new InternalApiSettingsSection().SectionName).Get<InternalApiSettingsSection>()
        ?? new InternalApiSettingsSection();
}
