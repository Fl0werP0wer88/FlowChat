namespace FlowChat.SocialGraphService.Infrastructure.Configuration;

public interface IApiSettingsManager
{
    JwtSettingsSection GetJwtSettingsSection();

    InternalApiSettingsSection GetInternalApiSettingsSection();
}
