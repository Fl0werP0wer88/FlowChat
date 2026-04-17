using FlowChat.Core.Contracts;

namespace FlowChat.NotificationService.Infrastructure.Configuration;

public sealed class ApiRuntimeSettingsSection : SettingsSectionBase
{
    public override string SectionName => "FlowChat";

    public string ApiUrl { get; set; } = "https://localhost:5000";
}
