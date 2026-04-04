using System.Security.Cryptography;
using FlowChat.AuthService.Application.Contracts.Infrastructure;

namespace FlowChat.AuthService.Infrastructure.Services;

public sealed class PasswordHashingService : IPasswordHashingService
{
    private const string Version = "v1";
    private const string Algorithm = "pbkdf2-sha512";
    private const int Iterations = 210_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA512,
            HashSize);

        return string.Join(
            '$',
            Version,
            Algorithm,
            Iterations.ToString(),
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    public PasswordVerificationResult VerifyHashedPassword(string hashedPassword, string providedPassword)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hashedPassword);
        ArgumentException.ThrowIfNullOrWhiteSpace(providedPassword);

        var segments = hashedPassword.Split('$', StringSplitOptions.TrimEntries);
        if (segments.Length != 5
            || !string.Equals(segments[0], Version, StringComparison.Ordinal)
            || !string.Equals(segments[1], Algorithm, StringComparison.Ordinal)
            || !int.TryParse(segments[2], out var iterations))
        {
            return PasswordVerificationResult.Failed;
        }

        try
        {
            var salt = Convert.FromBase64String(segments[3]);
            var expectedHash = Convert.FromBase64String(segments[4]);
            var computedHash = Rfc2898DeriveBytes.Pbkdf2(
                providedPassword,
                salt,
                iterations,
                HashAlgorithmName.SHA512,
                expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(computedHash, expectedHash)
                ? PasswordVerificationResult.Succeeded
                : PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            return PasswordVerificationResult.Failed;
        }
    }

    public string GenerateSecurityStamp()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }
}
