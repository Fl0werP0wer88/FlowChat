using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.AuthService.Infrastructure.Services;
using FluentAssertions;

namespace FlowChat.AuthService.UnitTests;

public sealed class PasswordHashingServiceTests
{
    [Fact]
    public void HashPassword_AndVerifyHashedPassword_WithMatchingPassword_ReturnsSucceeded()
    {
        var sut = new PasswordHashingService();

        var hash = sut.HashPassword("P@ssw0rd!");
        var result = sut.VerifyHashedPassword(hash, "P@ssw0rd!");

        hash.Should().StartWith("$argon2id$");
        result.Should().Be(PasswordVerificationResult.Succeeded);
    }

    [Fact]
    public void VerifyHashedPassword_WithWrongPassword_ReturnsFailed()
    {
        var sut = new PasswordHashingService();
        var hash = sut.HashPassword("P@ssw0rd!");

        var result = sut.VerifyHashedPassword(hash, "wrong-password");

        result.Should().Be(PasswordVerificationResult.Failed);
    }

    [Fact]
    public void VerifyHashedPassword_WithLegacyPbkdf2Hash_ReturnsFailed()
    {
        var sut = new PasswordHashingService();
        const string legacyHash = "v1$pbkdf2-sha512$210000$4i9EiIqlTLQSe+gJrbAtWQ==$8m0gP8INwQkQnyAPQWCF3s1t1mY4V8j+I0xSL2gSArM=";

        var result = sut.VerifyHashedPassword(legacyHash, "P@ssw0rd!");

        result.Should().Be(PasswordVerificationResult.Failed);
    }

    [Fact]
    public void GenerateSecurityStamp_ReturnsNonEmptyUniqueValues()
    {
        var sut = new PasswordHashingService();

        var first = sut.GenerateSecurityStamp();
        var second = sut.GenerateSecurityStamp();

        first.Should().NotBeNullOrWhiteSpace();
        second.Should().NotBeNullOrWhiteSpace();
        first.Should().NotBe(second);
    }
}
