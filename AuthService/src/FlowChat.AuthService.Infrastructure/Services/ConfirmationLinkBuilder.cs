using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.AuthService.Infrastructure.Configuration;

namespace FlowChat.AuthService.Infrastructure.Services;

public class ConfirmationLinkBuilder : IConfirmationLinkBuilder
{
    private readonly IApiSettingsManager _apiSettingsManager;

    public ConfirmationLinkBuilder(IApiSettingsManager apiSettingsManager)
    {
        _apiSettingsManager = apiSettingsManager;
    }

    public string BuildEmailConfirmationLink(Guid userId, string encodedToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(encodedToken);

        var baseUrl = _apiSettingsManager.GetConfirmationLinksSettings().EmailConfirmationBaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException("Missing configuration value: ConfirmationLinks:EmailConfirmationBaseUrl.");
        }

        var separator = baseUrl.Contains('?') ? "&" : "?";
        var userIdParam = Uri.EscapeDataString(userId.ToString());
        var tokenParam = Uri.EscapeDataString(encodedToken);

        return $"{baseUrl}{separator}userId={userIdParam}&token={tokenParam}";
    }
}
