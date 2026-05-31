using FlowChat.Shared.Domain;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationProcess;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class EmailVerificationProcessTests
{
    [Fact]
    public void Create_UsesEmailIdAsProcessId()
    {
        var userProfileId = Id<UserProfile>.New();
        var emailId = Id<Email>.New();

        var process = EmailVerificationProcess.Create(userProfileId, emailId);

        process.Id.Value.Should().Be(emailId.Value);
        process.UserProfileId.Should().Be(userProfileId);
        process.EmailId.Should().Be(emailId);
        process.Requests.Should().BeEmpty();
    }

    [Fact]
    public void IssueRequest_WhenNoRequestsExist_CreatesActiveRequest()
    {
        var nowUtc = new DateTimeOffset(2026, 4, 1, 8, 0, 0, TimeSpan.Zero);
        var process = CreateProcess();

        var request = process.IssueRequest(
            Id<EmailVerificationRequest>.New(),
            " nonce-123 ",
            nowUtc.AddMinutes(30),
            nowUtc);

        request.Nonce.Should().Be("nonce-123");
        request.IsActive(nowUtc).Should().BeTrue();
        process.Requests.Should().ContainSingle().Which.Should().BeSameAs(request);
    }

    [Fact]
    public void IssueRequest_WhenActiveRequestExists_InvalidatesExistingRequest()
    {
        var nowUtc = new DateTimeOffset(2026, 4, 1, 8, 0, 0, TimeSpan.Zero);
        var process = CreateProcess();
        var existingRequest = process.IssueRequest(
            Id<EmailVerificationRequest>.New(),
            "existing-nonce",
            nowUtc.AddHours(1),
            nowUtc);

        var newRequest = process.IssueRequest(
            Id<EmailVerificationRequest>.New(),
            "new-nonce",
            nowUtc.AddHours(1),
            nowUtc);

        existingRequest.InvalidatedAtUtc.Should().Be(nowUtc);
        newRequest.IsActive(nowUtc).Should().BeTrue();
        process.Requests.Count(x => x.IsActive(nowUtc)).Should().Be(1);
    }

    [Fact]
    public void IssueRequest_DoesNotInvalidateExpiredConsumedOrInvalidatedRequestsAgain()
    {
        var nowUtc = new DateTimeOffset(2026, 4, 1, 8, 0, 0, TimeSpan.Zero);
        var process = CreateProcess();
        var expiredRequest = process.IssueRequest(
            Id<EmailVerificationRequest>.New(),
            "expired-nonce",
            nowUtc.AddMinutes(-1),
            nowUtc);
        var activeRequest = process.IssueRequest(
            Id<EmailVerificationRequest>.New(),
            "active-nonce",
            nowUtc.AddMinutes(30),
            nowUtc);
        process.ConsumeRequest(activeRequest.Nonce, nowUtc);
        activeRequest.ConsumedAtUtc.Should().NotBeNull();
        var consumedAtUtc = activeRequest.ConsumedAtUtc.Value;

        process.IssueRequest(
            Id<EmailVerificationRequest>.New(),
            "new-nonce",
            nowUtc.AddMinutes(30),
            nowUtc);

        expiredRequest.InvalidatedAtUtc.Should().BeNull();
        activeRequest.ConsumedAtUtc.Should().Be(consumedAtUtc);
        process.Requests.Count(x => x.IsActive(nowUtc)).Should().Be(1);
    }

    [Fact]
    public void ConsumeRequest_WhenRequestAlreadyConsumed_DoesNotOverwriteConsumedAtUtc()
    {
        var firstConsumeAtUtc = new DateTimeOffset(2026, 4, 1, 8, 0, 0, TimeSpan.Zero);
        var secondConsumeAtUtc = firstConsumeAtUtc.AddMinutes(5);
        var process = CreateProcess();
        var request = process.IssueRequest(
            Id<EmailVerificationRequest>.New(),
            "nonce-123",
            firstConsumeAtUtc.AddHours(1),
            firstConsumeAtUtc);

        process.ConsumeRequest(request.Nonce, firstConsumeAtUtc);
        process.ConsumeRequest(request.Nonce, secondConsumeAtUtc);

        request.ConsumedAtUtc.Should().Be(firstConsumeAtUtc);
    }

    [Fact]
    public void IssueRequest_WithNonUtcExpiration_ThrowsArgumentException()
    {
        var process = CreateProcess();
        var act = () => process.IssueRequest(
            Id<EmailVerificationRequest>.New(),
            "nonce-123",
            new DateTimeOffset(2026, 4, 2, 8, 0, 0, TimeSpan.FromHours(1)),
            new DateTimeOffset(2026, 4, 1, 8, 0, 0, TimeSpan.Zero));

        act.Should().Throw<ArgumentException>()
            .WithMessage($"{FlowChat.Shared.Domain.ValueObjects.UtcDateTimeOffset.InvalidUtcDateTimeOffsetMessage}*");
    }

    private static EmailVerificationProcess CreateProcess()
    {
        return EmailVerificationProcess.Create(Id<UserProfile>.New(), Id<Email>.New());
    }
}
