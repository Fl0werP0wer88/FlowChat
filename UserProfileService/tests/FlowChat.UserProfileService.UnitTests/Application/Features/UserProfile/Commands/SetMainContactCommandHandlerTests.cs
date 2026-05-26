using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Application.Features.UserProfile.Commands.SetMainEmail;
using FlowChat.UserProfileService.Application.Features.UserProfile.Commands.SetMainPhone;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;
using FlowChat.UserProfileService.Domain.Entities.UserProfile.Events;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class SetMainContactCommandHandlerTests
{
    private readonly Mock<IUserProfileWriteRepository> _writeRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IDomainEventDispatcher> _dispatcherMock = new();
    private readonly SetMainEmailCommandHandler _emailHandler;
    private readonly SetMainPhoneCommandHandler _phoneHandler;

    public SetMainContactCommandHandlerTests()
    {
        _writeRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Id<UserProfile>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        _unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<Guid>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<Guid>>>, CancellationToken>(
                (operation, ct) => operation(ct));

        _emailHandler = new SetMainEmailCommandHandler(
            _writeRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _dispatcherMock.Object);

        _phoneHandler = new SetMainPhoneCommandHandler(
            _writeRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _dispatcherMock.Object);
    }

    private async Task<FlowChatResult<Guid>> SendAsync(SetMainEmailCommand command)
    {
        var validator = new SetMainEmailCommandValidator();
        var validationResult = await validator.ValidateAsync(command);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return FlowChatResult<Guid>.Failure(DomainError.Validation(errors: errors));
        }

        return await _emailHandler.Handle(command, CancellationToken.None);
    }

    private async Task<FlowChatResult<Guid>> SendAsync(SetMainPhoneCommand command)
    {
        var validator = new SetMainPhoneCommandValidator();
        var validationResult = await validator.ValidateAsync(command);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return FlowChatResult<Guid>.Failure(DomainError.Validation(errors: errors));
        }

        return await _phoneHandler.Handle(command, CancellationToken.None);
    }

    private static UserProfile CreateUserProfile()
    {
        var profile = UserProfile.Create(Id<UserProfile>.New(), "jdoe", EmailAddress.Create("john@example.com"));
        profile.ClearEvents();
        return profile;
    }

    [Fact]
    public async Task SetMainEmail_WhenEmailExists_SetsMainEmailAndDispatchesDomainEvents()
    {
        var profile = CreateUserProfile();
        var firstEmail = profile.Emails.Should().ContainSingle().Subject;
        var secondEmail = profile.AddEmail(Id<Email>.New(), EmailAddress.Create("john.secondary@example.com"));
        profile.ConfirmEmail(secondEmail.Id);
        profile.ClearEvents();

        _writeRepositoryMock
            .Setup(x => x.GetByIdAsync(profile.Id.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        List<IDomainEvent> dispatchedEvents = [];
        _dispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<IDomainEvent>, CancellationToken>((events, _) => dispatchedEvents.AddRange(events))
            .Returns(Task.CompletedTask);

        var result = await SendAsync(new SetMainEmailCommand(profile.Id.Value, secondEmail.Id.Value));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(secondEmail.Id.Value);
        firstEmail.IsMain.Should().BeFalse();
        secondEmail.IsMain.Should().BeTrue();

        var emailChangedEvent = dispatchedEvents.OfType<MainEmailChangedDomainEvent>().Should().ContainSingle().Subject;
        emailChangedEvent.UserProfileId.Should().Be(profile.Id);
        emailChangedEvent.EmailId.Should().Be(secondEmail.Id);
        emailChangedEvent.Address.Should().Be(secondEmail.Address);

        var stateChangedEvent = dispatchedEvents
            .OfType<AggregateStateChangedDomainEvent<UserProfile, UserProfileState>>()
            .Should().ContainSingle().Subject;
        stateChangedEvent.AggregateState.Emails.Should().ContainSingle(x =>
            x.Id == secondEmail.Id.Value &&
            x.Address == secondEmail.Address.Value &&
            x.IsMain &&
            x.IsConfirmed);
    }

    [Fact]
    public async Task SetMainEmail_WhenEmailDoesNotExist_ReturnsNotFound()
    {
        var profile = CreateUserProfile();
        _writeRepositoryMock
            .Setup(x => x.GetByIdAsync(profile.Id.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var result = await SendAsync(new SetMainEmailCommand(profile.Id.Value, Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task SetMainEmail_WhenEmailIdIsEmpty_ReturnsValidationFailure()
    {
        var result = await SendAsync(new SetMainEmailCommand(Guid.NewGuid(), Guid.Empty));

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Validation);
        result.Error.Errors.Should().Equal("EmailId is required.");
    }

    [Fact]
    public async Task SetMainEmail_WhenEmailIsNotConfirmed_ReturnsValidationFailure()
    {
        var profile = CreateUserProfile();
        var secondEmail = profile.AddEmail(Id<Email>.New(), EmailAddress.Create("john.secondary@example.com"));
        profile.ClearEvents();

        _writeRepositoryMock
            .Setup(x => x.GetByIdAsync(profile.Id.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var result = await SendAsync(new SetMainEmailCommand(profile.Id.Value, secondEmail.Id.Value));

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Validation);
        result.Error.ErrorMessage.Should().Be(
            $"Email '{secondEmail.Address.Value}' must be confirmed before it can be set as the main email.");
    }

    [Fact]
    public async Task SetMainEmail_WhenUserIdAndEmailIdAreEmpty_ReturnsValidationFailureWithBothErrors()
    {
        var result = await SendAsync(new SetMainEmailCommand(Guid.Empty, Guid.Empty));

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Validation);
        result.Error.ErrorMessage.Should().Be("Validation Failed.");
        result.Error.Errors.Should().Equal("UserId is required.", "EmailId is required.");
    }

    [Fact]
    public async Task SetMainPhone_WhenPhoneExists_SetsMainPhoneAndDispatchesDomainEvents()
    {
        var profile = CreateUserProfile();
        var firstPhone = profile.AddPhone(Id<Phone>.New(), PhoneNumber.Create("+48123123123"));
        var secondPhone = profile.AddPhone(Id<Phone>.New(), PhoneNumber.Create("+48987654321"));
        secondPhone.Confirm();
        profile.ClearEvents();

        _writeRepositoryMock
            .Setup(x => x.GetByIdAsync(profile.Id.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        List<IDomainEvent> dispatchedEvents = [];
        _dispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<IDomainEvent>, CancellationToken>((events, _) => dispatchedEvents.AddRange(events))
            .Returns(Task.CompletedTask);

        var result = await SendAsync(new SetMainPhoneCommand(profile.Id.Value, secondPhone.Id.Value));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(secondPhone.Id.Value);
        firstPhone.IsMain.Should().BeFalse();
        secondPhone.IsMain.Should().BeTrue();

        var phoneChangedEvent = dispatchedEvents.OfType<MainPhoneChangedDomainEvent>().Should().ContainSingle().Subject;
        phoneChangedEvent.UserProfileId.Should().Be(profile.Id);
        phoneChangedEvent.PhoneId.Should().Be(secondPhone.Id);
        phoneChangedEvent.Number.Should().Be(secondPhone.Number);

        var stateChangedEvent = dispatchedEvents
            .OfType<AggregateStateChangedDomainEvent<UserProfile, UserProfileState>>()
            .Should().ContainSingle().Subject;
        stateChangedEvent.AggregateState.Phones.Should().ContainSingle(x =>
            x.Id == secondPhone.Id.Value &&
            x.Number == secondPhone.Number.Value &&
            x.IsMain);
    }

    [Fact]
    public async Task SetMainPhone_WhenPhoneDoesNotExist_ReturnsNotFound()
    {
        var profile = CreateUserProfile();
        profile.AddPhone(Id<Phone>.New(), PhoneNumber.Create("+48123123123"));
        profile.ClearEvents();

        _writeRepositoryMock
            .Setup(x => x.GetByIdAsync(profile.Id.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var result = await SendAsync(new SetMainPhoneCommand(profile.Id.Value, Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task SetMainPhone_WhenPhoneIdIsEmpty_ReturnsValidationFailure()
    {
        var result = await SendAsync(new SetMainPhoneCommand(Guid.NewGuid(), Guid.Empty));

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Validation);
        result.Error.Errors.Should().Equal("PhoneId is required.");
    }

    [Fact]
    public async Task SetMainPhone_WhenPhoneIsNotConfirmed_ReturnsValidationFailure()
    {
        var profile = CreateUserProfile();
        profile.AddPhone(Id<Phone>.New(), PhoneNumber.Create("+48123123123"));
        var secondPhone = profile.AddPhone(Id<Phone>.New(), PhoneNumber.Create("+48987654321"));
        profile.ClearEvents();

        _writeRepositoryMock
            .Setup(x => x.GetByIdAsync(profile.Id.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var result = await SendAsync(new SetMainPhoneCommand(profile.Id.Value, secondPhone.Id.Value));

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Validation);
        result.Error.ErrorMessage.Should().Be(
            $"Phone '{secondPhone.Number.Value}' must be confirmed before it can be set as the main phone.");
    }

    [Fact]
    public async Task SetMainPhone_WhenUserIdAndPhoneIdAreEmpty_ReturnsValidationFailureWithBothErrors()
    {
        var result = await SendAsync(new SetMainPhoneCommand(Guid.Empty, Guid.Empty));

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Validation);
        result.Error.ErrorMessage.Should().Be("Validation Failed.");
        result.Error.Errors.Should().Equal("UserId is required.", "PhoneId is required.");
    }
}

