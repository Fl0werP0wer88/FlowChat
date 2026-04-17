using FlowChat.Core.Contracts;

namespace FlowChat.NotificationService.Consumers.Configuration;

public sealed class NotificationApiSettingsSection : SettingsSectionBase
{
    public override string SectionName => "NotificationApi";

    public string BaseUrl { get; set; } = "https://localhost:7206";

    public string ApiKey { get; set; } = string.Empty;
}
