using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.AuthService.Application.Features.User.Commands.ChangeAuthEmail;
using FlowChat.AuthService.Domain.Entities.Account;
using FlowChat.Core.Messaging;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FluentAssertions;
using MediatR;
using Moq;

namespace FlowChat.AuthService.UnitTests;

public sealed class ChangeAuthEmailCommandHandlerTests
{
    private readonly Mock<IAccountRepository> _accountRepositoryMock = new();
    private readonly Mock<IPasswordHashingService> _passwordHashingServiceMock = new();
    private readonly Mock<ILocalEventDispatcher> _domainEventDispatcherMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IAggregateBeforeSaveProcessor<ChangeAuthEmailCommand, Account>> _beforeSaveProcessorMock = new();
    private readonly ChangeAuthEmailCommandHandler _handler;

    public ChangeAuthEmailCommandHandlerTests()
    {
        _unitOfWorkMock
            .Setup(x => x.ExecuteCommandInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<Unit>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<Unit>>>, CancellationToken>(
                (operation, ct) => operation(ct));

        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _passwordHashingServiceMock
            .Setup(x => x.GenerateSecurityStamp())
            .Returns("new-security-stamp");

        _beforeSaveProcessorMock
            .Setup(x => x.ProcessAsync(
                It.IsAny<ChangeAuthEmailCommand>(),
                It.IsAny<Account>(),
                It.IsAny<AggregateState>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new ChangeAuthEmailCommandHandler(
            _accountRepositoryMock.Object,
            _passwordHashingServiceMock.Object,
            _domainEventDispatcherMock.Object,
            _unitOfWorkMock.Object,
            [_beforeSaveProcessorMock.Object]);
    }

    [Fact]
    public async Task Handle_WhenAccountExists_ChangesEmailAndRotatesSecurityStamp()
    {
        var account = Account.Restore(Guid.NewGuid(), "flower", EmailAddress.Create("flower@example.com"), "hash", "stamp", 0, true);
        List<IDomainEvent> dispatchedEvents = [];

        _accountRepositoryMock
            .Setup(x => x.GetByIdAsync(account.Id.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        _accountRepositoryMock
            .Setup(x => x.GetByEmailAsync(EmailAddress.Create("new@example.com"), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);
        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ILocalEvent>, CancellationToken>((events, _) => dispatchedEvents.AddRange(events.OfType<IDomainEvent>()))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(
            new ChangeAuthEmailCommand
            {
                UserId = account.Id.Value,
                EmailAddress = "new@example.com"
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        account.Email.Should().Be(EmailAddress.Create("new@example.com"));
        account.SecurityStamp.Should().Be("new-security-stamp");
        account.IsEmailConfirmed.Should().BeTrue();
        dispatchedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<AggregateStateChangedDomainEvent<Account, AccountSnapshot>>().Subject
            .AggregateState.Should().Be(new AccountSnapshot(
                account.Id.Value,
                account.FriendlyUserId.Value,
                "new@example.com",
                "new-security-stamp",
                0,
                true));
    }

    [Fact]
    public async Task Handle_WhenEmailBelongsToAnotherAccount_ReturnsConflict()
    {
        var account = Account.Restore(Guid.NewGuid(), "flower", EmailAddress.Create("flower@example.com"), "hash", "stamp", 0, true);
        var otherAccount = Account.Restore(Guid.NewGuid(), "other", EmailAddress.Create("new@example.com"), "hash", "stamp", 0, true);

        _accountRepositoryMock
            .Setup(x => x.GetByIdAsync(account.Id.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        _accountRepositoryMock
            .Setup(x => x.GetByEmailAsync(EmailAddress.Create("new@example.com"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(otherAccount);

        var result = await _handler.Handle(
            new ChangeAuthEmailCommand
            {
                UserId = account.Id.Value,
                EmailAddress = "new@example.com"
            },
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Conflict);
        _accountRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenEmailMatchesCurrentEmail_ReturnsSuccessWithoutUpdate()
    {
        var account = Account.Restore(Guid.NewGuid(), "flower", EmailAddress.Create("flower@example.com"), "hash", "stamp", 0, true);

        _accountRepositoryMock
            .Setup(x => x.GetByIdAsync(account.Id.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var result = await _handler.Handle(
            new ChangeAuthEmailCommand
            {
                UserId = account.Id.Value,
                EmailAddress = " flower@example.com "
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _passwordHashingServiceMock.Verify(x => x.GenerateSecurityStamp(), Times.Never);
        _accountRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()), Times.Never);
        _domainEventDispatcherMock.Verify(
            x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _beforeSaveProcessorMock.Verify(
            x => x.ProcessAsync(
                It.IsAny<ChangeAuthEmailCommand>(),
                It.IsAny<Account>(),
                It.IsAny<AggregateState>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserIdIsEmpty_ReturnsBadRequest()
    {
        var result = await _handler.Handle(
            new ChangeAuthEmailCommand
            {
                UserId = Guid.Empty,
                EmailAddress = "flower@example.com"
            },
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.BadRequest);
    }
}

