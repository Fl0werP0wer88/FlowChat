using FlowChat.Core.Contracts;

namespace FlowChat.UserProfileService.Infrastructure.Configuration;

public sealed class ConfirmationLinksSettingsSection : SettingsSectionBase
{
    public override string SectionName => "ConfirmationLinks";

    public string EmailVerificationBaseUrl { get; set; } = string.Empty;
}
