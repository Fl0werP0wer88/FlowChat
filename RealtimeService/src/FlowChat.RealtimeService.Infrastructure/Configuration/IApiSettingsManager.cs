namespace FlowChat.RealtimeService.Infrastructure.Configuration;

public interface IApiSettingsManager
{
    JwtSettingsSection GetJwtSettingsSection();

    ApiRuntimeSettingsSection GetApiRuntimeSettingsSection();

    InternalApiSettingsSection GetInternalApiSettingsSection();

    RealtimeConnectionsSettingsSection GetRealtimeConnectionsSettingsSection();

    PresenceServiceSettingsSection GetPresenceServiceSettingsSection();
}
