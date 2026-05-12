using FlowChat.Core.Contracts;

namespace FlowChat.RealtimeService.Infrastructure.Configuration.Settings;

public sealed class ChatServiceSettingsSection : SettingsSectionBase, IInternalApiSettingSection
{
    public override string SectionName => "ChatServiceApi";

    public string BaseUrl { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;
}
