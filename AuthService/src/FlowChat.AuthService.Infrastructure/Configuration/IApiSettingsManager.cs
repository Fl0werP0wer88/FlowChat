namespace FlowChat.AuthService.Infrastructure.Configuration;

public interface IApiSettingsManager
{
    JwtSettings GetJwtSettings();

    ApiRuntimeSettings GetApiRuntimeSettings();
}
