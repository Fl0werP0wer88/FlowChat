namespace FlowChat.AuthService.Application.Contracts.Infrastructure;

public interface IPasswordHashingService
{
    string HashPassword(string password);
    PasswordVerificationResult VerifyHashedPassword(string hashedPassword, string providedPassword);
    string GenerateSecurityStamp();
}
