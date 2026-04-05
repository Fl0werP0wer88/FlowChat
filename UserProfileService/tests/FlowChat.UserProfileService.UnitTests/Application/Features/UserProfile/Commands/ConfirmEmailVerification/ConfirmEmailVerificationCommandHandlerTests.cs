using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Application.Contracts.Infrastructure;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Application.Features.UserProfile.Commands.ConfirmEmailVerification;
using FlowChat.UserProfileService.Application.Features.UserProfile.EmailVerification;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;
using MediatR;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class ConfirmEmailVerificationCommandHandlerTests
{
    private readonly Mock<IUserProfileWriteRepository> _userProfileRepositoryMock = new();
    private readonly Mock<IEmailVerificationRequestWriteRepository> _verificationRequestRepositoryMock = new();
    private readonly Mock<IEmailVerificationTokenProtector> _tokenProtectorMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IDomainEventDispatcher> _dispatcherMock = new();
    private readonly ConfirmEmailVerificationCommandHandler _handler;

    public ConfirmEmailVerificationCommandHandlerTests()
    {
        // Default: token is tampered (unprotect returns false)
        EmailVerificationTokenPayload? nullPayload = null;
        _tokenProtectorMock
            .Setup(x => x.TryUnprotect(It.IsAny<string>(), out nullPayload))
            .Returns(false);

        _userProfileRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        _verificationRequestRepositoryMock
            .Setup(x => x.GetByNonceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailVerificationRequest?)null);

        _unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<Unit>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<Unit>>>, CancellationToken>((op, ct) => op(ct));

        _handler = new ConfirmEmailVerificationCommandHandler(
            _userProfileRepositoryMock.Object,
            _verificationRequestRepositoryMock.Object,
            _tokenProtectorMock.Object,
            _unitOfWorkMock.Object,
            _dispatcherMock.Object);
    }

    private async Task<FlowChatResult<Unit>> SendAsync(ConfirmEmailVerificationCommand command)
    {
        var validator = new ConfirmEmailVerificationCommandValidator();
        var validationResult = await validator.ValidateAsync(command);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return FlowChatResult<Unit>.Failure(DomainError.Validation(errors: errors));
        }

        return await _handler.Handle(command, CancellationToken.None);
    }

    private static UserProfile CreateUserProfile(string emailAddress)
    {
        var userProfile = UserProfile.Create("jdoe", "John Doe", EmailAddress.Create(emailAddress), id: Id<UserProfile>.New());
        userProfile.ClearEvents();
        return userProfile;
    }

    [Fact]
    public async Task Handle_WithValidToken_ConfirmsEmailAndConsumesRequest()
    {
        var profile = CreateUserProfile("john@example.com");
        var email = profile.Emails.Should().ContainSingle().Subject;
        var verificationRequest = EmailVerificationRequest.Create(
            profile.Id,
            email.Id,
            "valid-nonce",
            DateTimeOffset.UtcNow.AddHours(24));

        var payload = new EmailVerificationTokenPayload(profile.Id.Value, email.Id.Value, verificationRequest.Nonce);
        _tokenProtectorMock
            .Setup(x => x.TryUnprotect("valid-token", out payload))
            .Returns(true);

        _verificationRequestRepositoryMock
            .Setup(x => x.GetByNonceAsync("valid-nonce", It.IsAny<CancellationToken>()))
            .ReturnsAsync(verificationRequest);

        _userProfileRepositoryMock
            .Setup(x => x.GetByIdAsync(profile.Id.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var result = await SendAsync(new ConfirmEmailVerificationCommand("valid-token"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(Unit.Value);
        email.IsConfirmed.Should().BeTrue();
        verificationRequest.ConsumedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WithTamperedToken_ReturnsValidationFailure()
    {
        // Default setup: token protector returns false

        var result = await SendAsync(new ConfirmEmailVerificationCommand("invalid-token"));

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Validation);
        result.Error.ErrorMessage.Should().Be("Email verification link is invalid or has expired.");
    }

    [Fact]
    public async Task Handle_WithConsumedRequest_ReturnsValidationFailure()
    {
        var profile = CreateUserProfile("john@example.com");
        var email = profile.Emails.Should().ContainSingle().Subject;
        var verificationRequest = EmailVerificationRequest.Create(
            profile.Id,
            email.Id,
            "used-nonce",
            DateTimeOffset.UtcNow.AddHours(24));
        verificationRequest.Consume(DateTimeOffset.UtcNow);

        var payload = new EmailVerificationTokenPayload(profile.Id.Value, email.Id.Value, verificationRequest.Nonce);
        _tokenProtectorMock
            .Setup(x => x.TryUnprotect("used-token", out payload))
            .Returns(true);

        _verificationRequestRepositoryMock
            .Setup(x => x.GetByNonceAsync("used-nonce", It.IsAny<CancellationToken>()))
            .ReturnsAsync(verificationRequest);

        var result = await SendAsync(new ConfirmEmailVerificationCommand("used-token"));

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Validation);
        result.Error.ErrorMessage.Should().Be("Email verification link is invalid or has expired.");
    }

    [Fact]
    public async Task Handle_WithMissingProfile_ReturnsNotFound()
    {
        var userProfileId = Guid.NewGuid();
        var emailId = Guid.NewGuid();
        var verificationRequest = EmailVerificationRequest.Create(
            Id<UserProfile>.FromGuid(userProfileId),
            Id<Email>.FromGuid(emailId),
            "missing-profile-nonce",
            DateTimeOffset.UtcNow.AddHours(24));

        var payload = new EmailVerificationTokenPayload(userProfileId, emailId, verificationRequest.Nonce);
        _tokenProtectorMock
            .Setup(x => x.TryUnprotect("valid-token", out payload))
            .Returns(true);

        _verificationRequestRepositoryMock
            .Setup(x => x.GetByNonceAsync("missing-profile-nonce", It.IsAny<CancellationToken>()))
            .ReturnsAsync(verificationRequest);

        // userProfileRepositoryMock returns null by default

        var result = await SendAsync(new ConfirmEmailVerificationCommand("valid-token"));

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);
        result.Error.ErrorMessage.Should().Be($"User profile '{userProfileId}' was not found.");
    }

    [Fact]
    public async Task Handle_WithExpiredRequest_ReturnsValidationFailure()
    {
        var profile = CreateUserProfile("john@example.com");
        var email = profile.Emails.Should().ContainSingle().Subject;
        var verificationRequest = EmailVerificationRequest.Create(
            profile.Id,
            email.Id,
            "expired-nonce",
            DateTimeOffset.UtcNow.AddHours(-1)); // already expired

        var payload = new EmailVerificationTokenPayload(profile.Id.Value, email.Id.Value, verificationRequest.Nonce);
        _tokenProtectorMock
            .Setup(x => x.TryUnprotect("expired-token", out payload))
            .Returns(true);

        _verificationRequestRepositoryMock
            .Setup(x => x.GetByNonceAsync("expired-nonce", It.IsAny<CancellationToken>()))
            .ReturnsAsync(verificationRequest);

        var result = await SendAsync(new ConfirmEmailVerificationCommand("expired-token"));

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Validation);
        result.Error.ErrorMessage.Should().Be("Email verification link is invalid or has expired.");
    }
}
