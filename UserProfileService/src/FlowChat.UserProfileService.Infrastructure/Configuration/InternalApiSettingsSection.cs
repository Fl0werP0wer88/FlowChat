using FlowChat.Core.Contracts;

namespace FlowChat.UserProfileService.Infrastructure.Configuration;

public sealed class InternalApiSettingsSection : SettingsSectionBase
{
    public override string SectionName => "FlowChat:InternalApi";

    public string ApiKey { get; set; } = string.Empty;
}
