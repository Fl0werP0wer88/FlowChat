namespace FlowChat.UserProfileService.Infrastructure.Configuration;

public interface IApiSettingsManager
{
    ConfirmationLinksSettingsSection GetConfirmationLinksSettingsSection();

    InternalApiSettingsSection GetInternalApiSettingsSection();
}
