namespace FlowChat.UserProfileService.Infrastructure.Configuration;

public sealed class ConfirmationLinksSettingsSection
{
    public const string SectionName = "ConfirmationLinks";

    public string EmailVerificationBaseUrl { get; set; } = string.Empty;
}
