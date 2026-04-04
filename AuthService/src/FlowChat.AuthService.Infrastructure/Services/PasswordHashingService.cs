using System.Security.Cryptography;
using FlowChat.AuthService.Application.Contracts.Infrastructure;
using Isopoh.Cryptography.Argon2;

namespace FlowChat.AuthService.Infrastructure.Services;

public sealed class PasswordHashingService : IPasswordHashingService
{
    private const int TimeCost = 3;
    private const int MemoryCost = 65_536;
    private const int Parallelism = 1;
    private const int HashLength = 32;
    private const Argon2Type HashType = Argon2Type.HybridAddressing;

    public string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        return Argon2.Hash(
            password,
            timeCost: TimeCost,
            memoryCost: MemoryCost,
            parallelism: Parallelism,
            type: HashType,
            hashLength: HashLength);
    }

    public PasswordVerificationResult VerifyHashedPassword(string hashedPassword, string providedPassword)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hashedPassword);
        ArgumentException.ThrowIfNullOrWhiteSpace(providedPassword);

        try
        {
            return Argon2.Verify(hashedPassword, providedPassword, Parallelism)
                ? PasswordVerificationResult.Succeeded
                : PasswordVerificationResult.Failed;
        }
        catch (Exception)
        {
            return PasswordVerificationResult.Failed;
        }
    }

    public string GenerateSecurityStamp()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }
}
