namespace FlowChat.PresenceService.Infrastructure.Configuration;

public interface IApiSettingsManager
{
    JwtSettingsSection GetJwtSettingsSection();

    InternalApiSettingsSection GetInternalApiSettingsSection();

    PresenceStatusSettingsSection GetPresenceStatusSettingsSection();
}
