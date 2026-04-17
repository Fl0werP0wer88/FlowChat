namespace FlowChat.AuthService.Infrastructure.Configuration;

public interface IApiSettingsManager
{
    JwtSettingsSection GetJwtSettingsSection();

    InternalApiSettingsSection GetInternalApiSettingsSection();
}
