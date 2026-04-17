namespace FlowChat.NotificationService.Infrastructure.Configuration;

public interface IApiSettingsManager
{
    ApiRuntimeSettingsSection GetApiRuntimeSettingsSection();

    EmailSettingsSection GetEmailSettingsSection();

    InternalApiSettingsSection GetInternalApiSettingsSection();
}
