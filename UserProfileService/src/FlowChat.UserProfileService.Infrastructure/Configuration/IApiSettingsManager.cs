namespace FlowChat.UserProfileService.Infrastructure.Configuration;

public interface IApiSettingsManager
{
    ApiRuntimeSettings GetApiRuntimeSettings();

    InternalApiSettings GetInternalApiSettings();
}
