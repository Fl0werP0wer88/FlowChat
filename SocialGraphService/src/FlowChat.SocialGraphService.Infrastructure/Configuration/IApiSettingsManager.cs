namespace FlowChat.SocialGraphService.Infrastructure.Configuration;

public interface IApiSettingsManager
{
    JwtSettingsSection GetJwtSettingsSection();

    ApiRuntimeSettingsSection GetApiRuntimeSettingsSection();

    InternalApiSettingsSection GetInternalApiSettingsSection();
}
