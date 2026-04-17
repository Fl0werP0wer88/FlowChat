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
        _configuration.GetSection(new JwtSettingsSection().SectionName).Get<JwtSettingsSection>() ?? new JwtSettingsSection();

    public InternalApiSettingsSection GetInternalApiSettingsSection() =>
        _configuration.GetSection(new InternalApiSettingsSection().SectionName).Get<InternalApiSettingsSection>()
        ?? new InternalApiSettingsSection();
}
