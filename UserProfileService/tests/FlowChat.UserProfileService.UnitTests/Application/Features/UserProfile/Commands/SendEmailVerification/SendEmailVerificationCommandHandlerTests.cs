using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Application.Features.UserProfile.Commands.SendEmailVerification;
using FlowChat.UserProfileService.Application.Features.UserProfile.EmailVerification.Interfaces;
using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.Model;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class SendEmailVerificationCommandHandlerTests
{
    private readonly Mock<IUserProfileReadRepository> _readRepositoryMock = new();
    private readonly Mock<IEmailVerificationRequestIssuer> _issuerMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IDomainEventDispatcher> _dispatcherMock = new();
    private readonly SendEmailVerificationCommandHandler _handler;

    public SendEmailVerificationCommandHandlerTests()
    {
        _readRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfileDto?)null);

        _issuerMock
            .Setup(x => x.IssueAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid userProfileId, Guid emailId, string emailAddress, CancellationToken _) =>
                EmailVerificationRequest.Create(Id<EmailVerificationRequest>.New(), userProfileId, emailId, Guid.NewGuid().ToString("N"), DateTimeOffset.UtcNow.AddHours(24)));

        _unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<Guid>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<Guid>>>, CancellationToken>((op, ct) => op(ct));

        _handler = new SendEmailVerificationCommandHandler(
            _readRepositoryMock.Object,
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
        _issuerMock
            .Setup(x => x.IssueAsync(profileId, emailId, "john@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid upId, Guid eId, string _, CancellationToken _) =>
            {
                issuedRequest = EmailVerificationRequest.Create(Id<EmailVerificationRequest>.New(), upId, eId, Guid.NewGuid().ToString("N"), DateTimeOffset.UtcNow.AddHours(24));
                return issuedRequest;
            });

        var result = await SendAsync(new SendEmailVerificationCommand(profileId, emailId));

        result.IsSuccess.Should().BeTrue();
        issuedRequest.Should().NotBeNull();
        result.Value.Should().Be(issuedRequest!.Id.Value);
        _issuerMock.Verify(x => x.IssueAsync(profileId, emailId, "john@example.com", It.IsAny<CancellationToken>()), Times.Once);
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
        _issuerMock.Verify(x => x.IssueAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
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
        _issuerMock.Verify(x => x.IssueAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
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
        _issuerMock.Verify(x => x.IssueAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
