using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Application.Features.UserProfile.Commands.UpdateProfile;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;
using Moq;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class UpdateProfileCommandHandlerTests
{
    private readonly Mock<IUserProfileWriteRepository> _writeRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IDomainEventDispatcher> _dispatcherMock = new();
    private readonly UpdateProfileCommandHandler _handler;

    public UpdateProfileCommandHandlerTests()
    {
        _writeRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Id<UserProfile>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        _unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<Guid>>>>(),
                It.IsAny<Func<FlowChatResult<Guid>, CancellationToken, Task<FlowChatResult<Guid>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<
                Func<CancellationToken, Task<FlowChatResult<Guid>>>,
                Func<FlowChatResult<Guid>, CancellationToken, Task<FlowChatResult<Guid>>>,
                CancellationToken>(async (operation, beforeCommitOperation, ct) =>
                {
                    var result = await operation(ct);
                    return await beforeCommitOperation(result, ct);
                });

        _handler = new UpdateProfileCommandHandler(
            _writeRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _dispatcherMock.Object);
    }

    [Fact]
    public async Task Handle_WhenProfileExists_UpdatesAggregateAndDispatchesAggregateStateChangedEvent()
    {
        var profile = CreateUserProfile();
        _writeRepositoryMock
            .Setup(x => x.GetByIdAsync(profile.Id.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        List<IDomainEvent> dispatchedEvents = [];
        _dispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<IDomainEvent>, CancellationToken>((events, _) => dispatchedEvents.AddRange(events))
            .Returns(Task.CompletedTask);

        var result = await SendAsync(new UpdateProfileCommand(
            profile.Id.Value,
            " John ",
            " Doe ",
            " FlowChat ",
            " https://cdn.example/avatar.png ",
            " about me ",
            false));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(profile.Id.Value);
        profile.FirstName.Should().Be("John");
        profile.LastName.Should().Be("Doe");
        profile.Organization.Should().Be("FlowChat");
        profile.AvatarUrl.Should().Be("https://cdn.example/avatar.png");
        profile.Bio.Should().Be("about me");
        profile.IsActive.Should().BeFalse();

        var stateChangedEvent = dispatchedEvents
            .OfType<AggregateStateChangedDomainEvent<UserProfile, UserProfileState>>()
            .Should().ContainSingle().Subject;
        stateChangedEvent.AggregateState.FirstName.Should().Be("John");
        stateChangedEvent.AggregateState.LastName.Should().Be("Doe");
        stateChangedEvent.AggregateState.Organization.Should().Be("FlowChat");
        stateChangedEvent.AggregateState.AvatarUrl.Should().Be("https://cdn.example/avatar.png");
        stateChangedEvent.AggregateState.Bio.Should().Be("about me");
        stateChangedEvent.AggregateState.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenProfileDoesNotExist_ReturnsNotFoundFailure()
    {
        var result = await SendAsync(new UpdateProfileCommand(
            Guid.NewGuid(),
            "John",
            "Doe",
            "FlowChat",
            "https://cdn.example/avatar.png",
            "about me",
            true));

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);
        result.Error.ErrorMessage.Should().Contain("User profile");
    }

    [Fact]
    public async Task Handle_WhenFirstNameIsTooLong_ReturnsValidationFailure()
    {
        var result = await SendAsync(new UpdateProfileCommand(
            Guid.NewGuid(),
            new string('a', 101),
            null,
            null,
            null,
            null,
            true));

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Validation);
        result.Error.Errors.Should().Contain("FirstName must not exceed 100 characters.");
    }

    private async Task<FlowChatResult<Guid>> SendAsync(UpdateProfileCommand command)
    {
        var validator = new UpdateProfileCommandValidator();
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
        var profile = UserProfile.Create(Id<UserProfile>.New(), "jdoe", EmailAddress.Create("john@example.com"));
        profile.ClearEvents();
        return profile;
    }
}

