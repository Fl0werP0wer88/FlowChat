using FlowChat.Core.Contracts;

namespace FlowChat.RealtimeService.Infrastructure.Configuration.Settings;

public sealed class RealtimeInstancesSettingsSection : SettingsSectionBase
{
    public override string SectionName => "RealtimeApi";

    public Dictionary<string, string> Instances { get; set; } = new(StringComparer.Ordinal);
}
