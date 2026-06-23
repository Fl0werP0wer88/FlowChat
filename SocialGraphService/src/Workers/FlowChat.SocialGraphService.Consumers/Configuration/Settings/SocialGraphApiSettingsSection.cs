using FlowChat.Core.Contracts;

namespace FlowChat.SocialGraphService.Consumers.Configuration.Settings;

public sealed class SocialGraphApiSettingsSection : SettingsSectionBase, IInternalApiSettingSection
{
    public override string SectionName => "SocialGraphApi";

    public string BaseUrl { get; set; } = "https://localhost:7194";

    public string ApiKey { get; set; } = string.Empty;
}
