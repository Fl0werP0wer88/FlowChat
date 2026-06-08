using FlowChat.Core.Contracts;

namespace FlowChat.HarnessService.API.Configuration.Settings;

public sealed class InternalApiSettingsSection : SettingsSectionBase
{
    public override string SectionName => "FlowChat:InternalApi";

    public string ApiKey { get; set; } = string.Empty;
}
