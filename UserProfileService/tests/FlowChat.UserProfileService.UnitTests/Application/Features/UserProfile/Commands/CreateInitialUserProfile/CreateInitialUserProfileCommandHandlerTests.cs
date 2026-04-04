using AutoFixture;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Application.Features.UserProfile.Commands.CreateInitialUserProfile;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;
using FlowChat.UserProfileService.Domain.Entities.UserProfile.Events;
using FluentAssertions;
using Moq;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class CreateInitialUserProfileCommandHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IUserProfileReadRepository> _readRepositoryMock = new();
    private readonly Mock<IUserProfileWriteRepository> _writeRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IDomainEventDispatcher> _dispatcherMock = new();
    private readonly CreateInitialUserProfileCommandHandler _handler;

    public CreateInitialUserProfileCommandHandlerTests()
    {
        _readRepositoryMock
            .Setup(x => x.FriendlyUserIdExistsAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _readRepositoryMock
            .Setup(x => x.EmailAddressExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _writeRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile entity, CancellationToken _) => entity);

        _unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<Guid>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<Guid>>>, CancellationToken>((op, ct) => op(ct));

        _handler = new CreateInitialUserProfileCommandHandler(
            _readRepositoryMock.Object,
            _writeRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _dispatcherMock.Object);
    }

    // Runs validator then handler — mirrors the production MediatR pipeline
    private async Task<FlowChatResult<Guid>> SendAsync(CreateInitialUserProfileCommand command)
    {
        var validator = new CreateInitialUserProfileCommandValidator();
        var validationResult = await validator.ValidateAsync(command);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return FlowChatResult<Guid>.Failure(DomainError.Validation(errors: errors));
        }

        return await _handler.Handle(command, CancellationToken.None);
    }

    [Fact]
    public async Task Handle_WithEmailAndPhone_AddsContactsToAggregate()
    {
        var userId = _fixture.Create<Guid>();
        UserProfile? capturedProfile = null;
        _writeRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()))
            .Callback<UserProfile, CancellationToken>((entity, _) => capturedProfile = entity)
            .ReturnsAsync((UserProfile entity, CancellationToken _) => entity);

        var result = await SendAsync(
            new CreateInitialUserProfileCommand("jdoe", "John Doe", null, null, "john@example.com", "+48123123123", userId));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(userId);
        capturedProfile.Should().NotBeNull();
        capturedProfile.Emails.Should().ContainSingle()
            .Which.Should().Match<Email>(e => e.Address.Value == "john@example.com" && e.IsMain && e.IsAuth);
        capturedProfile.Phones.Should().ContainSingle()
            .Which.Should().Match<Phone>(p => p.Number.Value == "+48123123123" && p.IsMain);
    }

    [Fact]
    public async Task Handle_WithNullContacts_ReturnsValidationFailure()
    {
        var result = await SendAsync(
            new CreateInitialUserProfileCommand("jdoe", "John Doe", null, null, null, null, _fixture.Create<Guid>()));

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Validation);
        result.Error.Errors.Should().Equal("Email is required.");
        _writeRepositoryMock.Verify(x => x.AddAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithWhitespaceContacts_ReturnsValidationFailure()
    {
        var result = await SendAsync(
            new CreateInitialUserProfileCommand("jdoe", "John Doe", null, null, "   ", "   ", _fixture.Create<Guid>()));

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Validation);
        result.Error.Errors.Should().Equal("Email is required.");
        _writeRepositoryMock.Verify(x => x.AddAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithMissingRequiredFields_ReturnsValidationFailureWithAllErrorsInOrder()
    {
        var result = await SendAsync(
            new CreateInitialUserProfileCommand("   ", "   ", null, null, null, null, _fixture.Create<Guid>()));

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Validation);
        result.Error.ErrorMessage.Should().Be("Validation Failed.");
        result.Error.Errors.Should().Equal(
            "FriendlyUserId is required.",
            "DisplayName is required.",
            "Email is required.");
        _writeRepositoryMock.Verify(x => x.AddAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithSingleEmail_AddsMainEmailOnly()
    {
        UserProfile? capturedProfile = null;
        _writeRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()))
            .Callback<UserProfile, CancellationToken>((entity, _) => capturedProfile = entity)
            .ReturnsAsync((UserProfile entity, CancellationToken _) => entity);

        var result = await SendAsync(
            new CreateInitialUserProfileCommand("jdoe", "John Doe", null, null, "john@example.com", null, _fixture.Create<Guid>()));

        result.IsSuccess.Should().BeTrue();
        capturedProfile.Should().NotBeNull();
        capturedProfile.Emails.Should().ContainSingle()
            .Which.Should().Match<Email>(e => e.Address.Value == "john@example.com" && e.IsMain && e.IsAuth);
        capturedProfile.Phones.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithInvalidEmail_ReturnsValidationFailure()
    {
        var result = await SendAsync(
            new CreateInitialUserProfileCommand("jdoe", "John Doe", null, null, "not-an-email", null, _fixture.Create<Guid>()));

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Validation);
        result.Error.Errors.Should().Equal(EmailAddress.InvalidEmailAddressMessage);
        _writeRepositoryMock.Verify(x => x.AddAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithSinglePhone_ReturnsValidationFailureBecauseEmailIsRequired()
    {
        var result = await SendAsync(
            new CreateInitialUserProfileCommand("jdoe", "John Doe", null, null, null, "+48123123123", _fixture.Create<Guid>()));

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Validation);
        result.Error.Errors.Should().Equal("Email is required.");
        _writeRepositoryMock.Verify(x => x.AddAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithEmailAndFormattedPhone_NormalizesPhoneToE164()
    {
        UserProfile? capturedProfile = null;
        _writeRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()))
            .Callback<UserProfile, CancellationToken>((entity, _) => capturedProfile = entity)
            .ReturnsAsync((UserProfile entity, CancellationToken _) => entity);

        var result = await SendAsync(
            new CreateInitialUserProfileCommand("jdoe", "John Doe", null, null, "john@example.com", "+48 123 123 123", _fixture.Create<Guid>()));

        result.IsSuccess.Should().BeTrue();
        capturedProfile.Should().NotBeNull();
        capturedProfile.Phones.Should().ContainSingle()
            .Which.Number.Value.Should().Be("+48123123123");
    }

    [Fact]
    public async Task Handle_WithEmailAndInvalidPhone_ReturnsValidationFailure()
    {
        var result = await SendAsync(
            new CreateInitialUserProfileCommand("jdoe", "John Doe", null, null, "john@example.com", "123123123", _fixture.Create<Guid>()));

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Validation);
        result.Error.Errors.Should().Equal(PhoneNumber.InvalidPhoneNumberMessage);
        _writeRepositoryMock.Verify(x => x.AddAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithExistingFriendlyUserId_ReturnsConflictFailure()
    {
        _readRepositoryMock
            .Setup(x => x.FriendlyUserIdExistsAsync("jdoe", It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await SendAsync(
            new CreateInitialUserProfileCommand("jdoe", "John Doe", null, null, "john@example.com", null, _fixture.Create<Guid>()));

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Conflict);
        result.Error.ErrorMessage.Should().Contain("jdoe");
        _writeRepositoryMock.Verify(x => x.AddAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithExistingEmail_ReturnsConflictFailure()
    {
        _readRepositoryMock
            .Setup(x => x.EmailAddressExistsAsync("john@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await SendAsync(
            new CreateInitialUserProfileCommand("jdoe", "John Doe", null, null, "john@example.com", null, _fixture.Create<Guid>()));

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Conflict);
        result.Error.ErrorMessage.Should().Contain("john@example.com");
        _writeRepositoryMock.Verify(x => x.AddAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithAvatarUrlAndBio_SetsOptionalFields()
    {
        const string avatarUrl = "https://cdn.example.com/avatar.png";
        const string bio = "Software developer";
        UserProfile? capturedProfile = null;
        _writeRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()))
            .Callback<UserProfile, CancellationToken>((entity, _) => capturedProfile = entity)
            .ReturnsAsync((UserProfile entity, CancellationToken _) => entity);

        var result = await SendAsync(
            new CreateInitialUserProfileCommand("jdoe", "John Doe", avatarUrl, bio, "john@example.com", null, _fixture.Create<Guid>()));

        result.IsSuccess.Should().BeTrue();
        capturedProfile.Should().NotBeNull();
        capturedProfile.AvatarUrl.Should().Be(avatarUrl);
        capturedProfile.Bio.Should().Be(bio);
    }

    [Fact]
    public async Task Handle_WithLeadingTrailingWhitespace_TrimsAllFields()
    {
        UserProfile? capturedProfile = null;
        _writeRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()))
            .Callback<UserProfile, CancellationToken>((entity, _) => capturedProfile = entity)
            .ReturnsAsync((UserProfile entity, CancellationToken _) => entity);

        var result = await SendAsync(
            new CreateInitialUserProfileCommand("  jdoe  ", "  John Doe  ", null, null, "  john@example.com  ", null, _fixture.Create<Guid>()));

        result.IsSuccess.Should().BeTrue();
        capturedProfile.Should().NotBeNull();
        capturedProfile.FriendlyUserId.Should().Be("jdoe");
        capturedProfile.DisplayName.Should().Be("John Doe");
        capturedProfile.Emails.Should().ContainSingle()
            .Which.Address.Value.Should().Be("john@example.com");
    }

    [Fact]
    public async Task Handle_WithPhone_DomainEventContainsPhoneNumber()
    {
        IReadOnlyList<IDomainEvent> domainEventsAtAdd = [];

        _writeRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()))
            .Callback<UserProfile, CancellationToken>((entity, _) => domainEventsAtAdd = entity.DomainEvents.ToList())
            .ReturnsAsync((UserProfile entity, CancellationToken _) => entity);

        var result = await SendAsync(
            new CreateInitialUserProfileCommand("jdoe", "John Doe", null, null, "john@example.com", "+48123123123", _fixture.Create<Guid>()));

        result.IsSuccess.Should().BeTrue();

        var createdEvent = domainEventsAtAdd.OfType<UserProfileCreatedDomainEvent>().Should().ContainSingle().Subject;
        createdEvent.MainPhone.Should().NotBeNull();
        createdEvent.MainPhone.Value.Should().Be("+48123123123");

        var stateChangedEvent = domainEventsAtAdd
            .OfType<AggregateStateChangedDomainEvent<UserProfile, UserProfileSnapshot>>()
            .Should().ContainSingle().Subject;
        stateChangedEvent.AggregateState.MainPhone.Should().Be("+48123123123");
    }

    [Fact]
    public async Task Handle_AddsUserProfileCreatedDomainEventBeforeDispatchAndDispatchesIt()
    {
        IReadOnlyList<IDomainEvent> domainEventsAtAdd = [];
        List<IDomainEvent> dispatchedEvents = [];

        _writeRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()))
            .Callback<UserProfile, CancellationToken>((entity, _) => domainEventsAtAdd = entity.DomainEvents.ToList())
            .ReturnsAsync((UserProfile entity, CancellationToken _) => entity);

        _dispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<IDomainEvent>, CancellationToken>((events, _) => dispatchedEvents.AddRange(events))
            .Returns(Task.CompletedTask);

        var result = await SendAsync(
            new CreateInitialUserProfileCommand("jdoe", "John Doe", null, null, "john@example.com", null, _fixture.Create<Guid>()));

        result.IsSuccess.Should().BeTrue();

        var addedEvent = domainEventsAtAdd.OfType<UserProfileCreatedDomainEvent>().Should().ContainSingle().Subject;
        var dispatchedEvent = dispatchedEvents.OfType<UserProfileCreatedDomainEvent>().Should().ContainSingle().Subject;

        addedEvent.UserProfileId.Should().Be(dispatchedEvent.UserProfileId);
        addedEvent.MainEmailId.Should().Be(dispatchedEvent.MainEmailId);
        addedEvent.MainEmail.Value.Should().Be("john@example.com");
        addedEvent.MainPhone.Should().BeNull();

        var stateChangedEvent = domainEventsAtAdd
            .OfType<AggregateStateChangedDomainEvent<UserProfile, UserProfileSnapshot>>()
            .Should().ContainSingle().Subject;
        stateChangedEvent.AggregateState.MainEmail.Should().Be("john@example.com");
        stateChangedEvent.AggregateState.MainPhone.Should().BeNull();
    }
}
