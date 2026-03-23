namespace FlowChat.NotificationService.Infrastructure.Configuration;

public interface IApiSettingsManager
{
    ApiRuntimeSettings GetApiRuntimeSettings();

    EmailSettings GetEmailSettings();

    InternalApiSettings GetInternalApiSettings();
}
