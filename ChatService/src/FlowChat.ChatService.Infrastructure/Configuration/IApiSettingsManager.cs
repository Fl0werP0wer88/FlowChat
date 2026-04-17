namespace FlowChat.ChatService.Infrastructure.Configuration;

public interface IApiSettingsManager
{
    ApiRuntimeSettingsSection GetApiRuntimeSettingsSection();
}
