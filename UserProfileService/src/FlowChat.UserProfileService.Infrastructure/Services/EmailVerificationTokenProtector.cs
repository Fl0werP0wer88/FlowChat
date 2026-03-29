using System.Security.Cryptography;
using System.Text.Json;
using FlowChat.UserProfileService.Application.Contracts.Infrastructure;
using FlowChat.UserProfileService.Application.Features.UserProfiles.EmailVerification;
using Microsoft.AspNetCore.DataProtection;

namespace FlowChat.UserProfileService.Infrastructure.Services;

public sealed class EmailVerificationTokenProtector(IDataProtectionProvider dataProtectionProvider)
    : IEmailVerificationTokenProtector
{
    private const string EmailVerificationPurpose = "FlowChat.UserProfileService.EmailVerification.v1";
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(24);
    private readonly ITimeLimitedDataProtector _protector = dataProtectionProvider
        .CreateProtector(EmailVerificationPurpose)
        .ToTimeLimitedDataProtector();

    public string Protect(EmailVerificationTokenPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        var serializedPayload = JsonSerializer.Serialize(payload);
        return _protector.Protect(serializedPayload, TokenLifetime);
    }

    public bool TryUnprotect(string token, out EmailVerificationTokenPayload? payload)
    {
        payload = null;

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            var unprotectedValue = _protector.Unprotect(token.Trim(), out _);
            payload = JsonSerializer.Deserialize<EmailVerificationTokenPayload>(unprotectedValue);
            return payload is not null;
        }
        catch (Exception ex) when (
            ex is CryptographicException
            || ex is FormatException
            || ex is JsonException
            || ex is ArgumentException)
        {
            return false;
        }
    }
}
