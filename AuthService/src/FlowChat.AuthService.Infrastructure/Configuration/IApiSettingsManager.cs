namespace FlowChat.AuthService.Infrastructure.Configuration;

public interface IApiSettingsManager
{
    JwtSettings GetJwtSettings();

    ConfirmationLinksSettings GetConfirmationLinksSettings();

    ApiRuntimeSettings GetApiRuntimeSettings();
}
