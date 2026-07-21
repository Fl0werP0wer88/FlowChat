using FlowChat.Core.Results;
using FlowChat.Core.Messaging;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Application.Contracts.Infrastructure;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Application.Features.UserProfile.Commands.ConfirmEmailVerification;
using FlowChat.UserProfileService.Application.Features.UserProfile.EmailVerification;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationProcess;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class ConfirmEmailVerificationCommandHandlerTests
{
    private readonly Mock<IUserProfileWriteRepository> _userProfileRepositoryMock = new();
    private readonly Mock<IEmailVerificationProcessWriteRepository> _verificationProcessRepositoryMock = new();
    private readonly Mock<IEmailVerificationTokenProtector> _tokenProtectorMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ILocalEventDispatcher> _dispatcherMock = new();
    private readonly Mock<IAggregateBeforeSaveProcessorV2<ConfirmEmailVerificationCommand, UserProfile>> _beforeSaveProcessorMock = new();
    private readonly ConfirmEmailVerificationCommandHandler _handler;

    public ConfirmEmailVerificationCommandHandlerTests()
    {
        // Default: token is tampered (unprotect returns false)
        EmailVerificationTokenPayload? nullPayload = null;
        _tokenProtectorMock
            .Setup(x => x.TryUnprotect(It.IsAny<string>(), out nullPayload))
            .Returns(false);

        _userProfileRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Id<UserProfile>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        _verificationProcessRepositoryMock
            .Setup(x => x.GetByNonceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailVerificationProcess?)null);

        _verificationProcessRepositoryMock
            .Setup(x => x.GetConfirmationStateByNonceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailVerificationConfirmationState?)null);

        _unitOfWorkMock
            .Setup(x => x.ExecuteCommandInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<IdempotentCommandResult<Unit>>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<IdempotentCommandResult<Unit>>>>, CancellationToken>(
                (operation, ct) => operation(ct));

        _dispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _beforeSaveProcessorMock
            .Setup(x => x.ProcessAsync(
                It.IsAny<ConfirmEmailVerificationCommand>(),
                It.IsAny<UserProfile>(),
                It.IsAny<MutationType>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new ConfirmEmailVerificationCommandHandler(
            _userProfileRepositoryMock.Object,
            _verificationProcessRepositoryMock.Object,
            _tokenProtectorMock.Object,
            _unitOfWorkMock.Object,
            _dispatcherMock.Object,
            [_beforeSaveProcessorMock.Object]);
    }

    private async Task<FlowChatResult<IdempotentCommandResult<Unit>>> SendAsync(ConfirmEmailVerificationCommand command)
    {
        var validator = new ConfirmEmailVerificationCommandValidator();
        var validationResult = await validator.ValidateAsync(command);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return FlowChatResult<IdempotentCommandResult<Unit>>.Failure(DomainError.Validation(errors: errors));
        }

        return await _handler.Handle(command, CancellationToken.None);
    }

    private static UserProfile CreateUserProfile(string emailAddress)
    {
        var userProfile = UserProfile.Create(Id<UserProfile>.New(), "jdoe", EmailAddress.Create(emailAddress));
        userProfile.ClearEvents();
        return userProfile;
    }

    private static EmailVerificationProcess CreateProcess(Id<UserProfile> userProfileId, Id<Email> emailId)
    {
        return EmailVerificationProcess.Create(userProfileId, emailId);
    }

    [Fact]
    public async Task Handle_WithValidToken_ConfirmsEmailAndConsumesRequest()
    {
        var profile = CreateUserProfile("john@example.com");
        var email = profile.Emails.Should().ContainSingle().Subject;
        var process = CreateProcess(profile.Id, email.Id);
        var verificationRequest = process.IssueRequest(
            Id<EmailVerificationRequest>.New(),
            "valid-nonce",
            DateTimeOffset.UtcNow.AddHours(24),
            DateTimeOffset.UtcNow);

        var payload = new EmailVerificationTokenPayload(profile.Id.Value, email.Id.Value, verificationRequest.Nonce);
        _tokenProtectorMock
            .Setup(x => x.TryUnprotect("valid-token", out payload))
            .Returns(true);

        _verificationProcessRepositoryMock
            .Setup(x => x.GetByNonceAsync("valid-nonce", It.IsAny<CancellationToken>()))
            .ReturnsAsync(process);

        _userProfileRepositoryMock
            .Setup(x => x.GetByIdAsync(profile.Id.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var result = await SendAsync(new ConfirmEmailVerificationCommand("valid-token"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(Unit.Value);
        result.Value.WasAlreadyProcessed.Should().BeFalse();
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
    public async Task Handle_WithConsumedRequestAndConfirmedEmail_ReturnsSuccess()
    {
        var profile = CreateUserProfile("john@example.com");
        var email = profile.Emails.Should().ContainSingle().Subject;
        profile.ConfirmEmail(email.Id);
        profile.ClearEvents();
        var process = CreateProcess(profile.Id, email.Id);
        var verificationRequest = process.IssueRequest(
            Id<EmailVerificationRequest>.New(),
            "used-nonce",
            DateTimeOffset.UtcNow.AddHours(24),
            DateTimeOffset.UtcNow);
        process.ConsumeRequest(verificationRequest.Nonce, DateTimeOffset.UtcNow);

        var payload = new EmailVerificationTokenPayload(profile.Id.Value, email.Id.Value, verificationRequest.Nonce);
        _tokenProtectorMock
            .Setup(x => x.TryUnprotect("used-token", out payload))
            .Returns(true);

        _verificationProcessRepositoryMock
            .Setup(x => x.GetByNonceAsync("used-nonce", It.IsAny<CancellationToken>()))
            .ReturnsAsync(process);

        _userProfileRepositoryMock
            .Setup(x => x.GetByIdAsync(profile.Id.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var result = await SendAsync(new ConfirmEmailVerificationCommand("used-token"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(Unit.Value);
        result.Value.WasAlreadyProcessed.Should().BeTrue();
        _dispatcherMock.Verify(
            x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _beforeSaveProcessorMock.Verify(
            x => x.ProcessAsync(
                It.IsAny<ConfirmEmailVerificationCommand>(),
                It.IsAny<UserProfile>(),
                It.IsAny<MutationType>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithConsumedRequestAndUnconfirmedEmail_ReturnsValidationFailure()
    {
        var profile = CreateUserProfile("john@example.com");
        var email = profile.Emails.Should().ContainSingle().Subject;
        var process = CreateProcess(profile.Id, email.Id);
        var verificationRequest = process.IssueRequest(
            Id<EmailVerificationRequest>.New(),
            "used-unconfirmed-nonce",
            DateTimeOffset.UtcNow.AddHours(24),
            DateTimeOffset.UtcNow);
        process.ConsumeRequest(verificationRequest.Nonce, DateTimeOffset.UtcNow);

        var payload = new EmailVerificationTokenPayload(profile.Id.Value, email.Id.Value, verificationRequest.Nonce);
        _tokenProtectorMock
            .Setup(x => x.TryUnprotect("used-unconfirmed-token", out payload))
            .Returns(true);

        _verificationProcessRepositoryMock
            .Setup(x => x.GetByNonceAsync("used-unconfirmed-nonce", It.IsAny<CancellationToken>()))
            .ReturnsAsync(process);

        _userProfileRepositoryMock
            .Setup(x => x.GetByIdAsync(profile.Id.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var result = await SendAsync(new ConfirmEmailVerificationCommand("used-unconfirmed-token"));

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Validation);
        result.Error.ErrorMessage.Should().Be("Email verification link is invalid or has expired.");
    }

    [Fact]
    public async Task Handle_WithMissingProfile_ReturnsNotFound()
    {
        var userProfileId = Guid.NewGuid();
        var emailId = Guid.NewGuid();
        var process = CreateProcess(Id<UserProfile>.FromGuid(userProfileId), Id<Email>.FromGuid(emailId));
        var verificationRequest = process.IssueRequest(
            Id<EmailVerificationRequest>.New(),
            "missing-profile-nonce",
            DateTimeOffset.UtcNow.AddHours(24),
            DateTimeOffset.UtcNow);

        var payload = new EmailVerificationTokenPayload(userProfileId, emailId, verificationRequest.Nonce);
        _tokenProtectorMock
            .Setup(x => x.TryUnprotect("valid-token", out payload))
            .Returns(true);

        _verificationProcessRepositoryMock
            .Setup(x => x.GetByNonceAsync("missing-profile-nonce", It.IsAny<CancellationToken>()))
            .ReturnsAsync(process);

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
        var process = CreateProcess(profile.Id, email.Id);
        var verificationRequest = process.IssueRequest(
            Id<EmailVerificationRequest>.New(),
            "expired-nonce",
            DateTimeOffset.UtcNow.AddHours(-1),
            DateTimeOffset.UtcNow);

        var payload = new EmailVerificationTokenPayload(profile.Id.Value, email.Id.Value, verificationRequest.Nonce);
        _tokenProtectorMock
            .Setup(x => x.TryUnprotect("expired-token", out payload))
            .Returns(true);

        _verificationProcessRepositoryMock
            .Setup(x => x.GetByNonceAsync("expired-nonce", It.IsAny<CancellationToken>()))
            .ReturnsAsync(process);

        var result = await SendAsync(new ConfirmEmailVerificationCommand("expired-token"));

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Validation);
        result.Error.ErrorMessage.Should().Be("Email verification link is invalid or has expired.");
    }

    [Fact]
    public async Task Handle_WhenConcurrencyConflictButEmailWasConfirmedBySameToken_ReturnsSuccess()
    {
        var profile = CreateUserProfile("john@example.com");
        var email = profile.Emails.Should().ContainSingle().Subject;
        var process = CreateProcess(profile.Id, email.Id);
        var verificationRequest = process.IssueRequest(
            Id<EmailVerificationRequest>.New(),
            "concurrency-nonce",
            DateTimeOffset.UtcNow.AddHours(24),
            DateTimeOffset.UtcNow);

        var payload = new EmailVerificationTokenPayload(profile.Id.Value, email.Id.Value, verificationRequest.Nonce);
        _tokenProtectorMock
            .Setup(x => x.TryUnprotect("concurrency-token", out payload))
            .Returns(true);

        _verificationProcessRepositoryMock
            .Setup(x => x.GetByNonceAsync("concurrency-nonce", It.IsAny<CancellationToken>()))
            .ReturnsAsync(process);

        _verificationProcessRepositoryMock
            .Setup(x => x.GetConfirmationStateByNonceAsync("concurrency-nonce", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailVerificationConfirmationState(
                profile.Id.Value,
                email.Id.Value,
                InvalidatedAtUtc: null,
                ConsumedAtUtc: UtcDateTimeOffset.UtcNow,
                EmailIsConfirmed: true));

        _userProfileRepositoryMock
            .Setup(x => x.GetByIdAsync(profile.Id.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        _unitOfWorkMock
            .Setup(x => x.ExecuteCommandInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<IdempotentCommandResult<Unit>>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<IdempotentCommandResult<Unit>>>>, CancellationToken>(
                async (op, ct) =>
                {
                    await op(ct);
                    throw new DbUpdateConcurrencyException("Concurrency conflict.");
                });

        var result = await SendAsync(new ConfirmEmailVerificationCommand("concurrency-token"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(Unit.Value);
        result.Value.WasAlreadyProcessed.Should().BeTrue();
    }
}

