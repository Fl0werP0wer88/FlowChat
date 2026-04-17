namespace FlowChat.RealtimeService.Infrastructure.Configuration;

public interface IApiSettingsManager
{
    JwtSettingsSection GetJwtSettingsSection();

    InternalApiSettingsSection GetInternalApiSettingsSection();

    RealtimeConnectionsSettingsSection GetRealtimeConnectionsSettingsSection();

    PresenceServiceSettingsSection GetPresenceServiceSettingsSection();
}
