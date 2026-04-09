using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Application.Features.UserProfile.Commands.SetAuthEmail;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;
using Moq;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class SetAuthEmailCommandHandlerTests
{
    private readonly Mock<IUserProfileWriteRepository> _writeRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IDomainEventDispatcher> _dispatcherMock = new();
    private readonly SetAuthEmailCommandHandler _handler;

    public SetAuthEmailCommandHandlerTests()
    {
        _writeRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        _unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<Guid>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<Guid>>>, CancellationToken>((operation, ct) => operation(ct));

        _handler = new SetAuthEmailCommandHandler(
            _writeRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _dispatcherMock.Object);
    }

    [Fact]
    public async Task Handle_WhenEmailExists_SetsAuthEmailAndDispatchesAggregateStateChangedEvent()
    {
        var profile = CreateUserProfile();
        var initialAuthEmail = profile.Emails.Should().ContainSingle().Subject;
        var secondaryEmail = profile.AddEmail(EmailAddress.Create("john.secondary@example.com"));
        profile.ClearEvents();

        _writeRepositoryMock
            .Setup(x => x.GetByIdAsync(profile.Id.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        List<IDomainEvent> dispatchedEvents = [];
        _dispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<IDomainEvent>, CancellationToken>((events, _) => dispatchedEvents.AddRange(events))
            .Returns(Task.CompletedTask);

        var result = await SendAsync(new SetAuthEmailCommand(profile.Id.Value, secondaryEmail.Id.Value));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(secondaryEmail.Id.Value);
        initialAuthEmail.IsAuth.Should().BeFalse();
        secondaryEmail.IsAuth.Should().BeTrue();

        var stateChangedEvent = dispatchedEvents
            .OfType<AggregateStateChangedDomainEvent<UserProfile, UserProfileSnapshot>>()
            .Should().ContainSingle().Subject;
        stateChangedEvent.AggregateState.MainEmail.Should().Be(initialAuthEmail.Address.Value);
    }

    [Fact]
    public async Task Handle_WhenProfileDoesNotExist_ReturnsNotFoundFailure()
    {
        var result = await SendAsync(new SetAuthEmailCommand(Guid.NewGuid(), Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);
        result.Error.ErrorMessage.Should().Contain("User profile");
    }

    [Fact]
    public async Task Handle_WhenEmailDoesNotExist_ReturnsNotFoundFailure()
    {
        var profile = CreateUserProfile();
        _writeRepositoryMock
            .Setup(x => x.GetByIdAsync(profile.Id.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var result = await SendAsync(new SetAuthEmailCommand(profile.Id.Value, Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);
        result.Error.ErrorMessage.Should().Contain("Email");
    }

    [Fact]
    public async Task Handle_WhenEmailIdIsEmpty_ReturnsValidationFailure()
    {
        var result = await SendAsync(new SetAuthEmailCommand(Guid.NewGuid(), Guid.Empty));

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Validation);
        result.Error.Errors.Should().Equal("EmailId is required.");
    }

    private async Task<FlowChatResult<Guid>> SendAsync(SetAuthEmailCommand command)
    {
        var validator = new SetAuthEmailCommandValidator();
        var validationResult = await validator.ValidateAsync(command);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(error => error.ErrorMessage).ToList();
            return FlowChatResult<Guid>.Failure(DomainError.Validation(errors: errors));
        }

        return await _handler.Handle(command, CancellationToken.None);
    }

    private static UserProfile CreateUserProfile()
    {
        var profile = UserProfile.Create("jdoe", EmailAddress.Create("john@example.com"), id: Id<UserProfile>.New());
        profile.ClearEvents();
        return profile;
    }
}
