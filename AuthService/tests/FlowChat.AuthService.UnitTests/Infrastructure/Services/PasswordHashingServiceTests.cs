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

        hash.Should().Contain("pbkdf2-sha512");
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
