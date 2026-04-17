namespace FlowChat.AuthService.Infrastructure.Configuration;

public interface IApiSettingsManager
{
    JwtSettingsSection GetJwtSettingsSection();

    ApiRuntimeSettingsSection GetApiRuntimeSettingsSection();

    InternalApiSettingsSection GetInternalApiSettingsSection();
}
