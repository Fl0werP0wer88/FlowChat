using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.AuthService.Application.Features.User.Commands.RegisterUser;
using FlowChat.AuthService.Domain.Entities.Account;
using FlowChat.AuthService.Domain.Entities.Account.Events;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace FlowChat.AuthService.UnitTests;

public sealed class RegisterUserCommandHandlerTests
{
    private readonly Mock<IAccountRepository> _accountRepositoryMock = new();
    private readonly Mock<IPasswordHashingService> _passwordHashingServiceMock = new();
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

        _passwordHashingServiceMock.Setup(x => x.HashPassword("P@ssw0rd!")).Returns("hashed-password");
        _passwordHashingServiceMock.Setup(x => x.GenerateSecurityStamp()).Returns("security-stamp");

        _handler = new RegisterUserCommandHandler(
            _accountRepositoryMock.Object,
            _passwordHashingServiceMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object);
    }

    [Fact]
    public async Task Handle_WhenAccountIsNew_PersistsAccountAndDispatchesAccountRegisteredDomainEvent()
    {
        Account? persistedAccount = null;
        List<IDomainEvent> dispatchedEvents = [];

        _accountRepositoryMock
            .Setup(x => x.GetByEmailAsync("flower@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);
        _accountRepositoryMock
            .Setup(x => x.GetByFriendlyUserIdAsync("flower", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);
        _accountRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()))
            .Callback<Account, CancellationToken>((account, _) => persistedAccount = account)
            .Returns(Task.CompletedTask);
        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<IDomainEvent>, CancellationToken>((events, _) => dispatchedEvents.AddRange(events))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        persistedAccount.Should().NotBeNull();
        persistedAccount!.Email.Should().Be(EmailAddress.Create("flower@example.com"));
        persistedAccount.FriendlyUserId.Should().Be("flower");
        persistedAccount.PasswordHash.Should().Be("hashed-password");
        result.Value.Id.Should().Be(persistedAccount.Id.Value);
        dispatchedEvents.Should().ContainSingle(x => x is AccountRegisteredDomainEvent);
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyExists_ReturnsConflict()
    {
        _accountRepositoryMock
            .Setup(x => x.GetByEmailAsync("flower@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Account.Restore(Guid.NewGuid(), "other", EmailAddress.Create("flower@example.com"), "hash", "stamp", 0, false));

        var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Conflict);
        result.Error.ErrorMessage.Should().Be("Account with the provided email already exists.");
        _accountRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenFriendlyUserIdAlreadyExists_ReturnsConflict()
    {
        _accountRepositoryMock
            .Setup(x => x.GetByEmailAsync("flower@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);
        _accountRepositoryMock
            .Setup(x => x.GetByFriendlyUserIdAsync("flower", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Account.Restore(Guid.NewGuid(), "flower", EmailAddress.Create("other@example.com"), "hash", "stamp", 0, false));

        var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Conflict);
        result.Error.ErrorMessage.Should().Be("Account with the provided friendly user id already exists.");
        _accountRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static RegisterUserCommand CreateCommand() =>
        new()
        {
            FriendlyUserId = "flower",
            Email = "flower@example.com",
            Password = "P@ssw0rd!"
        };
}
