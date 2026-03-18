namespace FlowChat.RealtimeService.Infrastructure.Configuration;

public interface IApiSettingsManager
{
    JwtSettings GetJwtSettings();

    ApiRuntimeSettings GetApiRuntimeSettings();

    InternalApiSettings GetInternalApiSettings();

    RealtimeApiSettings GetRealtimeApiSettings();
}
