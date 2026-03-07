namespace FlowChat.AuthService.Infrastructure.Configuration;

public sealed class ConfirmationLinksSettings
{
    public const string SectionName = "ConfirmationLinks";

    public string EmailConfirmationBaseUrl { get; set; } = string.Empty;
}
