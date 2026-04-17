namespace FlowChat.NotificationService.Infrastructure.Configuration;

public interface IApiSettingsManager
{
    EmailSettingsSection GetEmailSettingsSection();

    InternalApiSettingsSection GetInternalApiSettingsSection();
}
