using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Application.Features.UserProfiles.Commands.AddPhone;
using FlowChat.UserProfileService.Domain.Entities;
using FlowChat.UserProfileService.Domain.Events;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class AddPhoneCommandHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IUserProfileWriteRepository> _writeRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IDomainEventDispatcher> _dispatcherMock = new();
    private readonly AddPhoneCommandHandler _handler;

    public AddPhoneCommandHandlerTests()
    {
        _writeRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        _unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<Guid>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<Guid>>>, CancellationToken>((op, ct) => op(ct));

        _handler = new AddPhoneCommandHandler(
            _writeRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _dispatcherMock.Object);
    }

    private async Task<FlowChatResult<Guid>> SendAsync(AddPhoneCommand command)
    {
        var validator = new AddPhoneCommandValidator();
        var validationResult = await validator.ValidateAsync(command);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return FlowChatResult<Guid>.Failure(DomainError.Validation(errors: errors));
        }

        return await _handler.Handle(command, CancellationToken.None);
    }

    private static UserProfile CreateProfile()
    {
        var profile = UserProfile.Create("jdoe", "John Doe", EmailAddress.Create("john@example.com"), id: Id<UserProfile>.New());
        profile.ClearEvents();
        return profile;
    }

    [Fact]
    public async Task Handle_WithEmptyUserIdAndMissingNumber_ReturnsSingleValidationFailureWithBothErrors()
    {
        var result = await SendAsync(new AddPhoneCommand(Guid.Empty, null));

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Validation);
        result.Error.ErrorMessage.Should().Be("Validation Failed.");
        result.Error.Errors.Should().Equal("UserId is required.", "Phone number is required.");
    }

    [Fact]
    public async Task Handle_WithInvalidNumber_ReturnsValidationFailure()
    {
        var profile = CreateProfile();
        _writeRepositoryMock
            .Setup(x => x.GetByIdAsync(profile.Id.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var result = await SendAsync(new AddPhoneCommand(profile.Id.Value, "123123123"));

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Validation);
        result.Error.Errors.Should().Equal(PhoneNumber.InvalidPhoneNumberMessage);
    }

    [Fact]
    public async Task Handle_WithFormattedDuplicateNumber_ReturnsConflict()
    {
        var profile = CreateProfile();
        profile.AddPhone("+48123123123");
        profile.ClearEvents();
        _writeRepositoryMock
            .Setup(x => x.GetByIdAsync(profile.Id.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var result = await SendAsync(new AddPhoneCommand(profile.Id.Value, "+48 123 123 123"));

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Conflict);
        result.Error.ErrorMessage.Should().Be("Phone '+48123123123' already exists.");
    }

    [Fact]
    public async Task Handle_WithValidNumber_AddsPhoneAndReturnsPhoneId()
    {
        var profile = CreateProfile();
        _writeRepositoryMock
            .Setup(x => x.GetByIdAsync(profile.Id.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var result = await SendAsync(new AddPhoneCommand(profile.Id.Value, "+48123123123"));

        result.IsSuccess.Should().BeTrue();
        var addedPhone = profile.Phones.Should().ContainSingle().Subject;
        result.Value.Should().Be(addedPhone.Id.Value);
        addedPhone.Number.Value.Should().Be("+48123123123");
    }

    [Fact]
    public async Task Handle_WithValidNumber_DispatchesDomainEvent()
    {
        var profile = CreateProfile();
        _writeRepositoryMock
            .Setup(x => x.GetByIdAsync(profile.Id.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        List<IDomainEvent> dispatchedEvents = [];
        _dispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<IDomainEvent>, CancellationToken>((events, _) => dispatchedEvents.AddRange(events))
            .Returns(Task.CompletedTask);

        var result = await SendAsync(new AddPhoneCommand(profile.Id.Value, "+48123123123"));

        result.IsSuccess.Should().BeTrue();
        dispatchedEvents.Should().NotBeEmpty();
    }
}
