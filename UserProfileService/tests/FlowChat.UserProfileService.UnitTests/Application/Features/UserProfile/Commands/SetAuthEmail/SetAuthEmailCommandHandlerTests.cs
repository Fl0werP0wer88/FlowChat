using FlowChat.Core.Messaging;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Application.Features.UserProfile.Commands.SetAuthEmail;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;
using FlowChat.UserProfileService.Domain.Entities.UserProfile.Events;
using FluentAssertions;
using Moq;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class SetAuthEmailCommandHandlerTests
{
    private readonly Mock<IUserProfileWriteRepository> _writeRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ILocalEventDispatcher> _dispatcherMock = new();
    private readonly Mock<IAggregateBeforeSaveProcessor<SetAuthEmailCommand, UserProfile>> _beforeSaveProcessorMock = new();
    private readonly SetAuthEmailCommandHandler _handler;

    public SetAuthEmailCommandHandlerTests()
    {
        _writeRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Id<UserProfile>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        _unitOfWorkMock
            .Setup(x => x.ExecuteCommandInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<Guid>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<Guid>>>, CancellationToken>(
                (operation, ct) => operation(ct));

        _beforeSaveProcessorMock
            .Setup(x => x.ProcessAsync(
                It.IsAny<SetAuthEmailCommand>(),
                It.IsAny<UserProfile>(),
                It.IsAny<AggregateState>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new SetAuthEmailCommandHandler(
            _writeRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _dispatcherMock.Object,
            [_beforeSaveProcessorMock.Object]);
    }

    [Fact]
    public async Task Handle_WhenEmailExists_SetsAuthEmailAndDispatchesAuthEmailChangedDomainEvent()
    {
        var profile = CreateUserProfile();
        var initialAuthEmail = profile.Emails.Should().ContainSingle().Subject;
        var secondaryEmail = profile.AddEmail(Id<Email>.New(), EmailAddress.Create("john.secondary@example.com"));
        profile.ConfirmEmail(secondaryEmail.Id);
        profile.ClearEvents();

        _writeRepositoryMock
            .Setup(x => x.GetByIdAsync(profile.Id.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        List<IDomainEvent> dispatchedEvents = [];
        _dispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ILocalEvent>, CancellationToken>((events, _) => dispatchedEvents.AddRange(events.OfType<IDomainEvent>()))
            .Returns(Task.CompletedTask);

        var result = await SendAsync(new SetAuthEmailCommand(profile.Id.Value, secondaryEmail.Id.Value));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(secondaryEmail.Id.Value);
        initialAuthEmail.IsAuth.Should().BeFalse();
        secondaryEmail.IsAuth.Should().BeTrue();

        dispatchedEvents.OfType<AuthEmailChangedDomainEvent>().Should().ContainSingle()
            .Which.Should().Match<AuthEmailChangedDomainEvent>(x =>
                x.UserProfileId == profile.Id &&
                x.EmailId == secondaryEmail.Id &&
                x.Address == secondaryEmail.Address);
        _beforeSaveProcessorMock.Verify(
            x => x.ProcessAsync(
                It.IsAny<SetAuthEmailCommand>(),
                profile,
                AggregateState.Updated,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEmailIsAlreadyAuth_ReturnsSuccessWithoutProcessingAggregateChanges()
    {
        var profile = CreateUserProfile();
        var authEmail = profile.Emails.Should().ContainSingle().Subject;
        var initialVersion = profile.Version;

        _writeRepositoryMock
            .Setup(x => x.GetByIdAsync(profile.Id.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var result = await SendAsync(new SetAuthEmailCommand(profile.Id.Value, authEmail.Id.Value));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(authEmail.Id.Value);
        profile.Version.Should().Be(initialVersion);
        _dispatcherMock.Verify(
            x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _beforeSaveProcessorMock.Verify(
            x => x.ProcessAsync(
                It.IsAny<SetAuthEmailCommand>(),
                It.IsAny<UserProfile>(),
                It.IsAny<AggregateState>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
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

    [Fact]
    public async Task Handle_WhenEmailIsNotConfirmed_ReturnsValidationFailure()
    {
        var profile = CreateUserProfile();
        var secondaryEmail = profile.AddEmail(Id<Email>.New(), EmailAddress.Create("john.secondary@example.com"));
        profile.ClearEvents();

        _writeRepositoryMock
            .Setup(x => x.GetByIdAsync(profile.Id.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var result = await SendAsync(new SetAuthEmailCommand(profile.Id.Value, secondaryEmail.Id.Value));

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Validation);
        result.Error.ErrorMessage.Should().Be(
            $"Email '{secondaryEmail.Address.Value}' must be confirmed before it can be set as the auth email.");
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
        var profile = UserProfile.Create(Id<UserProfile>.New(), "jdoe", EmailAddress.Create("john@example.com"));
        profile.ClearEvents();
        return profile;
    }
}

