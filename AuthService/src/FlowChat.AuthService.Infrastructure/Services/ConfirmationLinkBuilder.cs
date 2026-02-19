using FlowChat.AuthService.Application.Contracts.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace FlowChat.AuthService.Infrastructure.Services;

public class ConfirmationLinkBuilder : IConfirmationLinkBuilder
{
    private readonly IConfiguration _configuration;

    public ConfirmationLinkBuilder(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string BuildEmailConfirmationLink(Guid userId, string encodedToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(encodedToken);

        var baseUrl = _configuration["EmailSettings:ConfirmationBaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException("Missing configuration value: EmailSettings:ConfirmationBaseUrl.");
        }

        var separator = baseUrl.Contains('?') ? "&" : "?";
        var userIdParam = Uri.EscapeDataString(userId.ToString());
        var tokenParam = Uri.EscapeDataString(encodedToken);

        return $"{baseUrl}{separator}userId={userIdParam}&token={tokenParam}";
    }
}
