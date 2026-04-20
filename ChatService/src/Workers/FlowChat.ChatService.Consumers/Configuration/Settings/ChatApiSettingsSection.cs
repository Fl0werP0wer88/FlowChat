using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.Consumers.Configuration.Settings;

public sealed class ChatApiSettingsSection : SettingsSectionBase
{
    public override string SectionName => "ChatApi";

    public string BaseUrl { get; set; } = "https://localhost:5000";

    public string ApiKey { get; set; } = string.Empty;
}
