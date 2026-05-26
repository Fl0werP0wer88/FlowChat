using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.AuthService.Application.Features.User.Commands.RegisterUser;
using FlowChat.AuthService.Domain.Entities.Account;
using FlowChat.AuthService.Domain.Entities.Account.Events;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FlowChat.AuthService.UnitTests;

public sealed class RegisterUserCommandHandlerTests
{
    private readonly Mock<IAccountRepository> _accountRepositoryMock = new();
    private readonly Mock<IPasswordHashingService> _passwordHashingServiceMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IDomainEventDispatcher> _domainEventDispatcherMock = new();
    private readonly Mock<IDbUpdateExceptionClassifier> _dbUpdateExceptionClassifierMock = new();
    private readonly RegisterUserCommandHandler _handler;

    public RegisterUserCommandHandlerTests()
    {
        _unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<IdempotentCommandResult<RegisterUserCommandResponse>>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<IdempotentCommandResult<RegisterUserCommandResponse>>>>, CancellationToken>(
                (operation, ct) => operation(ct));

        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _passwordHashingServiceMock.Setup(x => x.HashPassword("P@ssw0rd!")).Returns("hashed-password");
        _passwordHashingServiceMock.Setup(x => x.GenerateSecurityStamp()).Returns("security-stamp");

        _handler = new RegisterUserCommandHandler(
            _accountRepositoryMock.Object,
            _passwordHashingServiceMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object,
            _dbUpdateExceptionClassifierMock.Object);
    }

    [Fact]
    public async Task Handle_WhenAccountIsNew_PersistsAccountAndDispatchesAccountRegisteredDomainEvent()
    {
        Account? persistedAccount = null;
        List<IDomainEvent> dispatchedEvents = [];
        var emailAddress = EmailAddress.Create("flower@example.com");

        _accountRepositoryMock
            .Setup(x => x.GetByEmailAsync(emailAddress, It.IsAny<CancellationToken>()))
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
        result.Value.WasAlreadyProcessed.Should().BeFalse();
        persistedAccount.Should().NotBeNull();
        persistedAccount!.Email.Should().Be(EmailAddress.Create("flower@example.com"));
        persistedAccount.FriendlyUserId.Value.Should().Be("flower");
        persistedAccount.PasswordHash.Should().Be("hashed-password");
        result.Value.Value.Id.Should().Be(persistedAccount.Id.Value);
        dispatchedEvents.Should().ContainSingle(x => x is AccountRegisteredDomainEvent);

        var registeredEvent = dispatchedEvents.OfType<AccountRegisteredDomainEvent>().Single();
        registeredEvent.FirstName.Should().Be("Flower");
        registeredEvent.LastName.Should().Be("Power");
        registeredEvent.Organization.Should().Be("FlowChat");
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyExists_ReturnsConflict()
    {
        var emailAddress = EmailAddress.Create("flower@example.com");

        _accountRepositoryMock
            .Setup(x => x.GetByEmailAsync(emailAddress, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Account.Restore(Guid.NewGuid(), "other", emailAddress, "hash", "stamp", 0, false));

        var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Conflict);
        result.Error.ErrorMessage.Should().Be("Account with the provided email already exists.");
        _accountRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAccountIdAlreadyExists_ReturnsExistingResponseWithoutDispatchingEvents()
    {
        var command = CreateCommand();
        var existingAccount = Account.Restore(
            command.Id,
            command.FriendlyUserId,
            EmailAddress.Create(command.Email),
            "hash",
            "stamp",
            0,
            false);
        List<IDomainEvent> dispatchedEvents = [];

        _accountRepositoryMock
            .Setup(x => x.GetByEmailAsync(EmailAddress.Create(command.Email), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);
        _accountRepositoryMock
            .Setup(x => x.GetByFriendlyUserIdAsync(command.FriendlyUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);
        _accountRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("duplicate"));
        _dbUpdateExceptionClassifierMock
            .Setup(x => x.IsIdempotencyConflict(It.IsAny<DbUpdateException>(), RegisterUserCommand.IdempotencyConflictKey))
            .Returns(true);
        _accountRepositoryMock
            .Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAccount);
        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<IDomainEvent>, CancellationToken>((events, _) => dispatchedEvents.AddRange(events))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.WasAlreadyProcessed.Should().BeTrue();
        result.Value.Value.Id.Should().Be(command.Id);
        dispatchedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenFriendlyUserIdAlreadyExists_ReturnsConflict()
    {
        var emailAddress = EmailAddress.Create("flower@example.com");

        _accountRepositoryMock
            .Setup(x => x.GetByEmailAsync(emailAddress, It.IsAny<CancellationToken>()))
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
            Id = Guid.NewGuid(),
            FriendlyUserId = "flower",
            Email = "flower@example.com",
            Password = "P@ssw0rd!",
            FirstName = "Flower",
            LastName = "Power",
            Organization = "FlowChat"
        };
}

