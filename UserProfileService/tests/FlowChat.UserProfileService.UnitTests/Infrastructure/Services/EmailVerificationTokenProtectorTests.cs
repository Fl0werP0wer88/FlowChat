using FlowChat.UserProfileService.Application.Features.UserProfile.EmailVerification;
using FlowChat.UserProfileService.Infrastructure.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class EmailVerificationTokenProtectorTests
{
    [Fact]
    public void Protect_AndTryUnprotect_RoundTripsPayload()
    {
        var sut = CreateSut();
        var payload = new EmailVerificationTokenPayload(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid().ToString("N"));

        var token = sut.Protect(payload);
        var success = sut.TryUnprotect(token, out var unprotectedPayload);

        success.Should().BeTrue();
        unprotectedPayload.Should().Be(payload);
    }

    [Fact]
    public void TryUnprotect_WithWhitespaceToken_ReturnsFalse()
    {
        var sut = CreateSut();

        var success = sut.TryUnprotect("   ", out var payload);

        success.Should().BeFalse();
        payload.Should().BeNull();
    }

    [Fact]
    public void TryUnprotect_WithTamperedToken_ReturnsFalse()
    {
        var sut = CreateSut();
        var token = sut.Protect(new EmailVerificationTokenPayload(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid().ToString("N")));

        var success = sut.TryUnprotect($"{token}tampered", out var payload);

        success.Should().BeFalse();
        payload.Should().BeNull();
    }

    private static EmailVerificationTokenProtector CreateSut()
    {
        var services = new ServiceCollection();
        services.AddDataProtection().UseEphemeralDataProtectionProvider();
        var provider = services.BuildServiceProvider();

        return new EmailVerificationTokenProtector(provider.GetRequiredService<IDataProtectionProvider>());
    }
}
