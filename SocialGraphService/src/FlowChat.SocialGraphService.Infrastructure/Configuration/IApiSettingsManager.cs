namespace FlowChat.SocialGraphService.Infrastructure.Configuration;

public interface IApiSettingsManager
{
    JwtSettings GetJwtSettings();

    ApiRuntimeSettings GetApiRuntimeSettings();

    InternalApiSettings GetInternalApiSettings();
}
