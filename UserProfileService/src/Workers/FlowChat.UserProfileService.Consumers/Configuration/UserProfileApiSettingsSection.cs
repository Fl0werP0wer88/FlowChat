using FlowChat.Core.Contracts;

namespace FlowChat.UserProfileService.Consumers.Configuration;

public sealed class UserProfileApiSettingsSection : SettingsSectionBase
{
    public override string SectionName => "UserProfileApi";

    public string BaseUrl { get; set; } = "https://localhost:7148";

    public string ApiKey { get; set; } = string.Empty;
}
