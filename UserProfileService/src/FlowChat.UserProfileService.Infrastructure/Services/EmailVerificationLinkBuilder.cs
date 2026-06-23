using FlowChat.UserProfileService.Application.Contracts.Infrastructure;
using FlowChat.UserProfileService.Infrastructure.Configuration.Settings;
using Microsoft.Extensions.Options;

namespace FlowChat.UserProfileService.Infrastructure.Services;

public sealed class EmailVerificationLinkBuilder(IOptions<ConfirmationLinksSettingsSection> confirmationLinksSettings)
    : IEmailVerificationLinkBuilder
{
    private readonly IOptions<ConfirmationLinksSettingsSection> _confirmationLinksSettings = confirmationLinksSettings;

    public string BuildEmailVerificationLink(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        var baseUrl = _confirmationLinksSettings.Value.EmailVerificationBaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException("Missing configuration value: ConfirmationLinks:EmailVerificationBaseUrl.");
        }

        var separator = baseUrl.Contains('?') ? "&" : "?";
        var encodedToken = Uri.EscapeDataString(token);

        return $"{baseUrl}{separator}token={encodedToken}";
    }
}
