namespace FlowChat.PresenceService.Infrastructure.Configuration;

public interface IApiSettingsManager
{
    JwtSettings GetJwtSettings();

    InternalApiSettings GetInternalApiSettings();

    PresenceStatusSettings GetPresenceStatusSettings();
}
