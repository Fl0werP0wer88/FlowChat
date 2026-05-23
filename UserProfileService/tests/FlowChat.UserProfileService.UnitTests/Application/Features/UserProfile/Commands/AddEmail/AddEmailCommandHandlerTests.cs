using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Application.Features.UserProfile.Commands.AddEmail;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;
using FlowChat.UserProfileService.Domain.Entities.UserProfile.Events;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class AddEmailCommandHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IUserProfileReadRepository> _readRepositoryMock = new();
    private readonly Mock<IUserProfileWriteRepository> _writeRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IDomainEventDispatcher> _dispatcherMock = new();
    private readonly Mock<IDbUpdateExceptionClassifier> _dbUpdateExceptionClassifierMock = new();
    private readonly AddEmailCommandHandler _handler;

    public AddEmailCommandHandlerTests()
    {
        _readRepositoryMock
            .Setup(x => x.EmailAddressExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _writeRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Id<UserProfile>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        _unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<IdempotentCommandResult<Guid>>>>>(),
                It.IsAny<Func<FlowChatResult<IdempotentCommandResult<Guid>>, CancellationToken, Task<FlowChatResult<IdempotentCommandResult<Guid>>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<
                Func<CancellationToken, Task<FlowChatResult<IdempotentCommandResult<Guid>>>>,
                Func<FlowChatResult<IdempotentCommandResult<Guid>>, CancellationToken, Task<FlowChatResult<IdempotentCommandResult<Guid>>>>,
                CancellationToken>(async (operation, beforeCommitOperation, ct) =>
                {
                    var result = await operation(ct);
                    return await beforeCommitOperation(result, ct);
                });

        _handler = new AddEmailCommandHandler(
            _readRepositoryMock.Object,
            _writeRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _dispatcherMock.Object,
            _dbUpdateExceptionClassifierMock.Object);
    }

    private async Task<FlowChatResult<IdempotentCommandResult<Guid>>> SendAsync(AddEmailCommand command)
    {
        var validator = new AddEmailCommandValidator();
        var validationResult = await validator.ValidateAsync(command);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return FlowChatResult<IdempotentCommandResult<Guid>>.Failure(DomainError.Validation(errors: errors));
        }

        return await _handler.Handle(command, CancellationToken.None);
    }

    private static UserProfile CreateProfile(string emailAddress)
    {
        var profile = UserProfile.Create(Id<UserProfile>.New(), "jdoe", EmailAddress.Create(emailAddress));
        profile.ClearEvents();
        return profile;
    }

    [Fact]
    public async Task Handle_WithEmptyUserIdAndMissingAddress_ReturnsSingleValidationFailureWithBothErrors()
    {
        var result = await SendAsync(new AddEmailCommand(Guid.Empty, Guid.NewGuid(), null));

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Validation);
        result.Error.ErrorMessage.Should().Be("Validation Failed.");
        result.Error.Errors.Should().Equal("UserId is required.", "Email address is required.");
    }

    [Fact]
    public async Task Handle_WithInvalidAddress_ReturnsValidationFailure()
    {
        var profile = CreateProfile("primary@example.com");
        _writeRepositoryMock
            .Setup(x => x.GetByIdAsync(profile.Id.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var result = await SendAsync(new AddEmailCommand(profile.Id.Value, Guid.NewGuid(), "not-an-email"));

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Validation);
        result.Error.Errors.Should().Equal(EmailAddress.InvalidEmailAddressMessage);
    }

    [Fact]
    public async Task Handle_WithDuplicateAddressIgnoringCase_ReturnsConflict()
    {
        var profile = CreateProfile("john@example.com");
        _writeRepositoryMock
            .Setup(x => x.GetByIdAsync(profile.Id.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _readRepositoryMock
            .Setup(x => x.EmailAddressExistsAsync("john@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await SendAsync(new AddEmailCommand(profile.Id.Value, Guid.NewGuid(), "JOHN@example.com"));

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Conflict);
        result.Error.ErrorMessage.Should().Be("Email 'john@example.com' is already taken.");
    }

    [Fact]
    public async Task Handle_WithUniqueAddress_AddsEmailAndReturnsEmailId()
    {
        var profile = CreateProfile("primary@example.com");
        _writeRepositoryMock
            .Setup(x => x.GetByIdAsync(profile.Id.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var result = await SendAsync(new AddEmailCommand(profile.Id.Value, Guid.NewGuid(), "secondary@example.com"));

        result.IsSuccess.Should().BeTrue();
        var addedEmail = profile.Emails.Single(x => x.Address.Value == "secondary@example.com");
        result.Value.WasAlreadyProcessed.Should().BeFalse();
        result.Value.Value.Should().Be(addedEmail.Id.Value);
    }

    [Fact]
    public async Task Handle_WithUniqueAddress_DispatchesEmailAddedDomainEvent()
    {
        var profile = CreateProfile("primary@example.com");
        _writeRepositoryMock
            .Setup(x => x.GetByIdAsync(profile.Id.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        List<IDomainEvent> dispatchedEvents = [];
        _dispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<IDomainEvent>, CancellationToken>((events, _) => dispatchedEvents.AddRange(events))
            .Returns(Task.CompletedTask);

        var result = await SendAsync(new AddEmailCommand(profile.Id.Value, Guid.NewGuid(), "secondary@example.com"));

        result.IsSuccess.Should().BeTrue();
        result.Value.WasAlreadyProcessed.Should().BeFalse();
        var addedEmail = profile.Emails.Single(x => x.Address.Value == "secondary@example.com");
        var emailAddedEvent = dispatchedEvents.OfType<EmailAddedDomainEvent>().Should().ContainSingle().Subject;
        emailAddedEvent.UserProfileId.Should().Be(profile.Id);
        emailAddedEvent.EmailId.Should().Be(addedEmail.Id);
        emailAddedEvent.Email.Value.Should().Be("secondary@example.com");
    }

    [Fact]
    public async Task Handle_WhenEmailIdAlreadyExists_ReturnsExistingResponseWithoutDispatchingEvents()
    {
        var profile = CreateProfile("primary@example.com");
        var existingEmail = profile.AddEmail(Id<Email>.New(), EmailAddress.Create("secondary@example.com"));
        profile.ClearEvents();
        List<IDomainEvent> dispatchedEvents = [];

        _writeRepositoryMock
            .Setup(x => x.GetByIdAsync(profile.Id.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _readRepositoryMock
            .Setup(x => x.EmailAddressExistsAsync("secondary@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _dbUpdateExceptionClassifierMock
            .Setup(x => x.IsExpectedIdempotencyConflict(It.IsAny<DbUpdateException>(), AddEmailCommand.IdempotencyConflictKey))
            .Returns(true);
        _dispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<IDomainEvent>, CancellationToken>((events, _) => dispatchedEvents.AddRange(events))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<IdempotentCommandResult<Guid>>>>>(),
                It.IsAny<Func<FlowChatResult<IdempotentCommandResult<Guid>>, CancellationToken, Task<FlowChatResult<IdempotentCommandResult<Guid>>>>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("duplicate"));

        var result = await SendAsync(new AddEmailCommand(profile.Id.Value, existingEmail.Id.Value, "secondary@example.com"));

        result.IsSuccess.Should().BeTrue();
        result.Value.WasAlreadyProcessed.Should().BeTrue();
        result.Value.Value.Should().Be(existingEmail.Id.Value);
        dispatchedEvents.Should().BeEmpty();
    }
}

