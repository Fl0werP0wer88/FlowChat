using FlowChat.Shared.Domain;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class EmailVerificationRequestTests
{
    [Fact]
    public void Create_WithUtcExpiration_CreatesRequest()
    {
        var userProfileId = Id<UserProfile>.New();
        var emailId = Id<Email>.New();
        var expiresAtUtc = new DateTimeOffset(2026, 4, 2, 8, 0, 0, TimeSpan.Zero);

        var request = EmailVerificationRequest.Create(userProfileId, emailId, " nonce-123 ", expiresAtUtc);

        request.UserProfileId.Should().Be(userProfileId);
        request.EmailId.Should().Be(emailId);
        request.Nonce.Should().Be("nonce-123");
        request.ExpiresAtUtc.Should().Be(expiresAtUtc);
        request.InvalidatedAtUtc.Should().BeNull();
        request.ConsumedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Create_WithNonUtcExpiration_ThrowsArgumentException()
    {
        var act = () => EmailVerificationRequest.Create(
            Id<UserProfile>.New(),
            Id<Email>.New(),
            "nonce-123",
            new DateTimeOffset(2026, 4, 2, 8, 0, 0, TimeSpan.FromHours(1)));

        act.Should().Throw<ArgumentException>()
            .WithMessage($"{FlowChat.Shared.Domain.ValueObjects.UtcDateTimeOffset.InvalidUtcDateTimeOffsetMessage}*");
    }

    [Fact]
    public void IsActive_WhenRequestIsNotConsumedInvalidatedOrExpired_ReturnsTrue()
    {
        var nowUtc = new DateTimeOffset(2026, 4, 1, 8, 0, 0, TimeSpan.Zero);
        var request = EmailVerificationRequest.Create(
            Id<UserProfile>.New(),
            Id<Email>.New(),
            "nonce-123",
            nowUtc.AddMinutes(30));

        var isActive = request.IsActive(nowUtc);

        isActive.Should().BeTrue();
        request.IsExpired(nowUtc).Should().BeFalse();
    }

    [Fact]
    public void Invalidate_WhenRequestIsActive_MarksItInvalidated()
    {
        var nowUtc = new DateTimeOffset(2026, 4, 1, 8, 0, 0, TimeSpan.Zero);
        var request = EmailVerificationRequest.Create(
            Id<UserProfile>.New(),
            Id<Email>.New(),
            "nonce-123",
            nowUtc.AddMinutes(30));

        request.Invalidate(nowUtc);

        request.InvalidatedAtUtc.Should().Be(nowUtc);
        request.IsActive(nowUtc).Should().BeFalse();
    }

    [Fact]
    public void Consume_WhenRequestAlreadyConsumed_DoesNotOverwriteConsumedAtUtc()
    {
        var firstConsumeAtUtc = new DateTimeOffset(2026, 4, 1, 8, 0, 0, TimeSpan.Zero);
        var secondConsumeAtUtc = firstConsumeAtUtc.AddMinutes(5);
        var request = EmailVerificationRequest.Create(
            Id<UserProfile>.New(),
            Id<Email>.New(),
            "nonce-123",
            firstConsumeAtUtc.AddHours(1));

        request.Consume(firstConsumeAtUtc);
        request.Consume(secondConsumeAtUtc);

        request.ConsumedAtUtc.Should().Be(firstConsumeAtUtc);
    }

    [Fact]
    public void IsActive_WithNonUtcArgument_ThrowsArgumentException()
    {
        var request = EmailVerificationRequest.Create(
            Id<UserProfile>.New(),
            Id<Email>.New(),
            "nonce-123",
            new DateTimeOffset(2026, 4, 1, 9, 0, 0, TimeSpan.Zero));

        var act = () => request.IsActive(new DateTimeOffset(2026, 4, 1, 8, 0, 0, TimeSpan.FromHours(1)));

        act.Should().Throw<ArgumentException>()
            .WithMessage($"{FlowChat.Shared.Domain.ValueObjects.UtcDateTimeOffset.InvalidUtcDateTimeOffsetMessage}*");
    }
}
