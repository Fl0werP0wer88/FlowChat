using FlowChat.Core.Contracts;

namespace FlowChat.HarnessService.Consumers.Configuration.Settings;

public sealed class HarnessApiSettingsSection : SettingsSectionBase, IInternalApiSettingSection
{
    public override string SectionName => "HarnessApi";

    public string BaseUrl { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;
}
