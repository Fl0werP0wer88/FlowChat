using FlowChat.Core.Contracts;

namespace FlowChat.RealtimeService.Infrastructure.Configuration.Settings;

public sealed class InternalApiSettingsSection : SettingsSectionBase, IInternalApiSettingSection
{
    public override string SectionName => "FlowChat:InternalApi";

    public string ApiKey { get; set; } = string.Empty;

    string? IInternalApiSettingSection.BaseUrl => null;
}
