using FlowChat.Core.Contracts;

namespace FlowChat.SocialGraphService.Consumers.Configuration;

public sealed class SocialGraphApiSettingsSection : SettingsSectionBase
{
    public override string SectionName => "SocialGraphApi";

    public string BaseUrl { get; set; } = "https://localhost:7194";

    public string ApiKey { get; set; } = string.Empty;
}
