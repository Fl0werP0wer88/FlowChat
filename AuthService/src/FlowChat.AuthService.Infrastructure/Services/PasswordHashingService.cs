using System.Security.Cryptography;
using FlowChat.AuthService.Application.Contracts.Infrastructure;
using Isopoh.Cryptography.Argon2;

namespace FlowChat.AuthService.Infrastructure.Services;

public sealed class PasswordHashingService : IPasswordHashingService
{
    private const int TimeCost = 3;
    // 64 MB — intentionally high to make GPU/ASIC brute-force attacks expensive.
    private const int MemoryCost = 65_536;
    private const int Parallelism = 1;
    private const int HashLength = 32;
    // Argon2id: hybrid mode combining side-channel resistance (Argon2i) and GPU resistance (Argon2d).
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
            // Malformed hash strings throw rather than returning false — treat as verification failure
            // so callers never have to distinguish between "wrong password" and "corrupt hash".
            return PasswordVerificationResult.Failed;
        }
    }

    public string GenerateSecurityStamp()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }
}
