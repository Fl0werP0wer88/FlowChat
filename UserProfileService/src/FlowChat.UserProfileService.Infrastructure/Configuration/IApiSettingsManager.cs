namespace FlowChat.UserProfileService.Infrastructure.Configuration;

public interface IApiSettingsManager
{
    ApiRuntimeSettings GetApiRuntimeSettings();
    ConfirmationLinksSettings GetConfirmationLinksSettings();

    InternalApiSettings GetInternalApiSettings();
}
