using FlowChat.UserProfileService.Application.Contracts.Infrastructure;
using FlowChat.UserProfileService.Infrastructure.Configuration;

namespace FlowChat.UserProfileService.Infrastructure.Services;

public sealed class EmailVerificationLinkBuilder(IApiSettingsManager apiSettingsManager)
    : IEmailVerificationLinkBuilder
{
    private readonly IApiSettingsManager _apiSettingsManager = apiSettingsManager;

    public string BuildEmailVerificationLink(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        var baseUrl = _apiSettingsManager.GetConfirmationLinksSettingsSection().EmailVerificationBaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException("Missing configuration value: ConfirmationLinks:EmailVerificationBaseUrl.");
        }

        var separator = baseUrl.Contains('?') ? "&" : "?";
        var encodedToken = Uri.EscapeDataString(token);

        return $"{baseUrl}{separator}token={encodedToken}";
    }
}
