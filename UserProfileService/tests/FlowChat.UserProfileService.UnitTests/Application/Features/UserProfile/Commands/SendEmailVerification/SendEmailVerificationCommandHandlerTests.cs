using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Application.Features.UserProfile.Commands.SendEmailVerification;
using FlowChat.UserProfileService.Application.Features.UserProfile.EmailVerification.Interfaces;
using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.Model;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationProcess;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class SendEmailVerificationCommandHandlerTests
{
    private readonly Mock<IUserProfileReadRepository> _readRepositoryMock = new();
    private readonly Mock<IEmailVerificationProcessWriteRepository> _verificationProcessRepositoryMock = new();
    private readonly Mock<IEmailVerificationRequestIssuer> _issuerMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ILocalEventDispatcher> _dispatcherMock = new();
    private readonly SendEmailVerificationCommandHandler _handler;

    public SendEmailVerificationCommandHandlerTests()
    {
        _readRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfileDto?)null);

        _verificationProcessRepositoryMock
            .Setup(x => x.GetByEmailIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailVerificationProcess?)null);

        _verificationProcessRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<EmailVerificationProcess>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailVerificationProcess entity, CancellationToken _) => entity);

        _issuerMock
            .Setup(x => x.IssueAsync(
                It.IsAny<EmailVerificationProcess>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailVerificationProcess _, Guid userProfileId, Guid emailId, string _, CancellationToken _) =>
                CreateRequest(userProfileId, emailId));

        _unitOfWorkMock
            .Setup(x => x.ExecuteCommandInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<Guid>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<Guid>>>, CancellationToken>(
                (operation, ct) => operation(ct));

        _handler = new SendEmailVerificationCommandHandler(
            _readRepositoryMock.Object,
            _verificationProcessRepositoryMock.Object,
            _issuerMock.Object,
            _unitOfWorkMock.Object,
            _dispatcherMock.Object);
    }

    private async Task<FlowChatResult<Guid>> SendAsync(SendEmailVerificationCommand command)
    {
        var validator = new SendEmailVerificationCommandValidator();
        var validationResult = await validator.ValidateAsync(command);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return FlowChatResult<Guid>.Failure(DomainError.Validation(errors: errors));
        }

        return await _handler.Handle(command, CancellationToken.None);
    }

    private static UserProfileDto CreateUserProfileDto(Guid profileId, string email, bool isConfirmed = false) =>
        new(
            profileId,
            "jdoe",
            null,
            null,
            null,
            null,
            null,
            true,
            null,
            [new EmailDto(Guid.NewGuid(), EmailAddress.Create(email).Value, true, true, isConfirmed, true)],
            []);

    [Fact]
    public async Task Handle_WithUnconfirmedEmail_IssuesVerificationRequest()
    {
        var profileId = Guid.NewGuid();
        var dto = CreateUserProfileDto(profileId, "john@example.com");
        var emailId = dto.Emails[0].Id;

        _readRepositoryMock
            .Setup(x => x.GetByIdAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        EmailVerificationRequest? issuedRequest = null;
        EmailVerificationProcess? addedProcess = null;
        EmailVerificationProcess? issuedProcess = null;

        _verificationProcessRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<EmailVerificationProcess>(), It.IsAny<CancellationToken>()))
            .Callback<EmailVerificationProcess, CancellationToken>((process, _) => addedProcess = process)
            .ReturnsAsync((EmailVerificationProcess entity, CancellationToken _) => entity);

        _issuerMock
            .Setup(x => x.IssueAsync(
                It.IsAny<EmailVerificationProcess>(),
                profileId,
                emailId,
                "john@example.com",
                It.IsAny<CancellationToken>()))
            .Callback<EmailVerificationProcess, Guid, Guid, string, CancellationToken>((process, _, _, _, _) => issuedProcess = process)
            .ReturnsAsync((EmailVerificationProcess _, Guid upId, Guid eId, string _, CancellationToken _) =>
            {
                issuedRequest = CreateRequest(upId, eId);
                return issuedRequest;
            });

        var result = await SendAsync(new SendEmailVerificationCommand(profileId, emailId));

        result.IsSuccess.Should().BeTrue();
        issuedRequest.Should().NotBeNull();
        addedProcess.Should().NotBeNull();
        addedProcess!.Id.Value.Should().Be(emailId);
        issuedProcess.Should().BeSameAs(addedProcess);
        result.Value.Should().Be(issuedRequest!.Id.Value);
        _verificationProcessRepositoryMock.Verify(x => x.GetByEmailIdAsync(emailId, It.IsAny<CancellationToken>()), Times.Once);
        _verificationProcessRepositoryMock.Verify(x => x.AddAsync(It.IsAny<EmailVerificationProcess>(), It.IsAny<CancellationToken>()), Times.Once);
        _issuerMock.Verify(x => x.IssueAsync(
            It.IsAny<EmailVerificationProcess>(),
            profileId,
            emailId,
            "john@example.com",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithExistingProcess_PassesExistingProcessToIssuer()
    {
        var profileId = Guid.NewGuid();
        var dto = CreateUserProfileDto(profileId, "john@example.com");
        var emailId = dto.Emails[0].Id;
        var process = EmailVerificationProcess.Create(
            Id<UserProfile>.FromGuid(profileId),
            Id<Email>.FromGuid(emailId));

        _readRepositoryMock
            .Setup(x => x.GetByIdAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        _verificationProcessRepositoryMock
            .Setup(x => x.GetByEmailIdAsync(emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(process);

        EmailVerificationProcess? issuedProcess = null;
        _issuerMock
            .Setup(x => x.IssueAsync(
                process,
                profileId,
                emailId,
                "john@example.com",
                It.IsAny<CancellationToken>()))
            .Callback<EmailVerificationProcess, Guid, Guid, string, CancellationToken>((capturedProcess, _, _, _, _) => issuedProcess = capturedProcess)
            .ReturnsAsync((EmailVerificationProcess _, Guid upId, Guid eId, string _, CancellationToken _) =>
                CreateRequest(upId, eId));

        var result = await SendAsync(new SendEmailVerificationCommand(profileId, emailId));

        result.IsSuccess.Should().BeTrue();
        issuedProcess.Should().BeSameAs(process);
        _verificationProcessRepositoryMock.Verify(x => x.GetByEmailIdAsync(emailId, It.IsAny<CancellationToken>()), Times.Once);
        _verificationProcessRepositoryMock.Verify(x => x.AddAsync(It.IsAny<EmailVerificationProcess>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithConfirmedEmail_ReturnsValidationFailure()
    {
        var profileId = Guid.NewGuid();
        var dto = CreateUserProfileDto(profileId, "john@example.com", isConfirmed: true);
        var emailId = dto.Emails[0].Id;

        _readRepositoryMock
            .Setup(x => x.GetByIdAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await SendAsync(new SendEmailVerificationCommand(profileId, emailId));

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Validation);
        result.Error.ErrorMessage.Should().Be($"Email 'john@example.com' is already confirmed.");
        _issuerMock.Verify(x => x.IssueAsync(
            It.IsAny<EmailVerificationProcess>(),
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithMissingProfile_ReturnsNotFound()
    {
        var profileId = Guid.NewGuid();
        var emailId = Guid.NewGuid();

        // default setup returns null

        var result = await SendAsync(new SendEmailVerificationCommand(profileId, emailId));

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);
        _issuerMock.Verify(x => x.IssueAsync(
            It.IsAny<EmailVerificationProcess>(),
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithEmailNotInProfile_ReturnsNotFound()
    {
        var profileId = Guid.NewGuid();
        var dto = CreateUserProfileDto(profileId, "john@example.com");
        var wrongEmailId = Guid.NewGuid();

        _readRepositoryMock
            .Setup(x => x.GetByIdAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await SendAsync(new SendEmailVerificationCommand(profileId, wrongEmailId));

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);
        _issuerMock.Verify(x => x.IssueAsync(
            It.IsAny<EmailVerificationProcess>(),
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private static EmailVerificationRequest CreateRequest(Guid userProfileId, Guid emailId)
    {
        return EmailVerificationProcess
            .Create(Id<UserProfile>.FromGuid(userProfileId), Id<Email>.FromGuid(emailId))
            .IssueRequest(
                Id<EmailVerificationRequest>.New(),
                Guid.NewGuid().ToString("N"),
                DateTimeOffset.UtcNow.AddHours(24),
                DateTimeOffset.UtcNow);
    }
}

