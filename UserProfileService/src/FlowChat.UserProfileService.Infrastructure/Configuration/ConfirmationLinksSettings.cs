namespace FlowChat.UserProfileService.Infrastructure.Configuration;

public sealed class ConfirmationLinksSettings
{
    public const string SectionName = "ConfirmationLinks";

    public string EmailVerificationBaseUrl { get; set; } = string.Empty;
}
