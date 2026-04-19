using FlowChat.Core.Contracts;
using FlowChat.UserProfileService.Application.Contracts.Infrastructure;
using FlowChat.UserProfileService.Infrastructure.Configuration.Settings;

namespace FlowChat.UserProfileService.Infrastructure.Services;

public sealed class EmailVerificationLinkBuilder(ISettingsProvider settingsProvider)
    : IEmailVerificationLinkBuilder
{
    private readonly ISettingsProvider _settingsProvider = settingsProvider;

    public string BuildEmailVerificationLink(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        var baseUrl = _settingsProvider.GetSection<ConfirmationLinksSettingsSection>().EmailVerificationBaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException("Missing configuration value: ConfirmationLinks:EmailVerificationBaseUrl.");
        }

        var separator = baseUrl.Contains('?') ? "&" : "?";
        var encodedToken = Uri.EscapeDataString(token);

        return $"{baseUrl}{separator}token={encodedToken}";
    }
}
