using AutoFixture;
using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.AuthService.Application.Features.User.Commands.RegisterUser;
using FlowChat.AuthService.Domain.Entities.Identity;
using FlowChat.AuthService.Domain.Entities.Identity.Events;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FluentAssertions;
using Moq;

namespace FlowChat.AuthService.UnitTests;

public sealed class RegisterUserCommandHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IIdentityRepository> _identityRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IDomainEventDispatcher> _domainEventDispatcherMock = new();
    private readonly RegisterUserCommandHandler _handler;

    public RegisterUserCommandHandlerTests()
    {
        _unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<RegisterUserCommandResponse>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<RegisterUserCommandResponse>>>, CancellationToken>((operation, ct) => operation(ct));

        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new RegisterUserCommandHandler(
            _identityRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object);
    }

    [Fact]
    public async Task Handle_CreatesUser_AndDispatchesAccountRegisteredDomainEvent()
    {
        Identity? createdUser = null;
        List<IDomainEvent> dispatchedEvents = [];

        _identityRepositoryMock
            .Setup(x => x.CreateUserAsync(It.IsAny<Identity>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<Identity, string, CancellationToken>((user, _, _) => createdUser = user)
            .ReturnsAsync((Identity user, string _, CancellationToken _) => user.Id.Value);

        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<IDomainEvent>, CancellationToken>((events, _) => dispatchedEvents.AddRange(events))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        createdUser.Should().NotBeNull();
        result.Value.Id.Should().Be(createdUser!.Id.Value);
        createdUser.EmailConfirmed.Should().BeFalse();
        createdUser.AccountConfirmed.Should().BeFalse();

        dispatchedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<AccountRegisteredDomainEvent>()
            .Which.UserId.Value.Should().Be(createdUser.Id.Value);
    }

    [Fact]
    public async Task Handle_WhenRepositoryReportsDuplicateUserName_ReturnsConflictFailure()
    {
        _identityRepositoryMock
            .Setup(x => x.CreateUserAsync(It.IsAny<Identity>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(
                "User creation failed: DuplicateUserName: Username 'flower' is already taken."));

        var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Conflict);
        result.Error.ErrorMessage.Should().Be("User with the provided username or email already exists.");
        result.Error.Errors.Should().Contain("Username 'flower' is already taken.");
        _domainEventDispatcherMock.Verify(
            x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRepositoryReportsDuplicateEmail_ReturnsConflictFailure()
    {
        _identityRepositoryMock
            .Setup(x => x.CreateUserAsync(It.IsAny<Identity>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(
                "User creation failed: DuplicateEmail: Email 'flower@example.com' is already taken."));

        var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Conflict);
        result.Error.ErrorMessage.Should().Be("User with the provided username or email already exists.");
        result.Error.Errors.Should().Contain("Email 'flower@example.com' is already taken.");
        _domainEventDispatcherMock.Verify(
            x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRepositoryReportsValidationErrors_ReturnsValidationFailure()
    {
        _identityRepositoryMock
            .Setup(x => x.CreateUserAsync(It.IsAny<Identity>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(
                "User creation failed: PasswordTooShort: Password must be at least 8 characters.; PasswordRequiresUpper: Password must contain an uppercase letter."));

        var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Validation);
        result.Error.ErrorMessage.Should().Be("User registration validation failed.");
        result.Error.Errors.Should().Contain("Password must be at least 8 characters.");
        result.Error.Errors.Should().Contain("Password must contain an uppercase letter.");
        _domainEventDispatcherMock.Verify(
            x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRepositoryThrowsUnexpectedInvalidOperationException_RethrowsIt()
    {
        _identityRepositoryMock
            .Setup(x => x.CreateUserAsync(It.IsAny<Identity>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed."));

        var act = () => _handler.Handle(CreateCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database connection failed.");

        _domainEventDispatcherMock.Verify(
            x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private RegisterUserCommand CreateCommand()
    {
        return new RegisterUserCommand
        {
            UserName = "flower",
            Email = "flower@example.com",
            PhoneNumber = "+48123123123",
            Password = "P@ssw0rd!"
        };
    }
}
