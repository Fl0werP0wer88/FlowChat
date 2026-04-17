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

    private TSettings ResolveSection<TSettings>(string sectionName)
        where TSettings : new()
    {
        return _configuration.GetSection(sectionName).Get<TSettings>() ?? new TSettings();
    }
}
