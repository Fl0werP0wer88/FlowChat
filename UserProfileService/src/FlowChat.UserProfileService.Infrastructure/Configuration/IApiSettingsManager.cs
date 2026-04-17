namespace FlowChat.UserProfileService.Infrastructure.Configuration;

public interface IApiSettingsManager
{
    ApiRuntimeSettingsSection GetApiRuntimeSettingsSection();
    ConfirmationLinksSettingsSection GetConfirmationLinksSettingsSection();

    InternalApiSettingsSection GetInternalApiSettingsSection();
}
