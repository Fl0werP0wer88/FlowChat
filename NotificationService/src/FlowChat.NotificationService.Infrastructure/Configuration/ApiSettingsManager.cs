using Microsoft.Extensions.Configuration;

namespace FlowChat.NotificationService.Infrastructure.Configuration;

public sealed class ApiSettingsManager : IApiSettingsManager
{
    private readonly IConfiguration _configuration;

    public ApiSettingsManager(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public EmailSettingsSection GetEmailSettingsSection() =>
        _configuration.GetSection(new EmailSettingsSection().SectionName).Get<EmailSettingsSection>() ?? new EmailSettingsSection();

    public InternalApiSettingsSection GetInternalApiSettingsSection() =>
        _configuration.GetSection(new InternalApiSettingsSection().SectionName).Get<InternalApiSettingsSection>()
        ?? new InternalApiSettingsSection();
}
