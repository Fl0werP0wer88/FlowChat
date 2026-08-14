using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.AuthService.Application.Features.User.Commands.ConfirmAuthEmail;
using FlowChat.AuthService.Domain.Entities.Account;
using FlowChat.AuthService.Domain.Entities.Account.Events;
using FlowChat.Core.Messaging;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FluentAssertions;
using MediatR;
using Moq;

namespace FlowChat.AuthService.UnitTests;

public sealed class ConfirmAuthEmailCommandHandlerTests
{
    private readonly Mock<IAccountRepository> _accountRepositoryMock = new();
    private readonly Mock<ILocalEventDispatcher> _domainEventDispatcherMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly ConfirmAuthEmailCommandHandler _handler;

    public ConfirmAuthEmailCommandHandlerTests()
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

        _handler = new ConfirmAuthEmailCommandHandler(
            _accountRepositoryMock.Object,
            _domainEventDispatcherMock.Object,
            _unitOfWorkMock.Object,
            []);
    }

    [Fact]
    public async Task Handle_WhenAccountExists_ConfirmsEmailAndDispatchesAccountConfirmedDomainEvent()
    {
        var emailAddress = EmailAddress.Create("flower@example.com");
        var account = Account.Restore(Guid.NewGuid(), "flower", emailAddress, "hash", "stamp", 0, false);
        List<IDomainEvent> dispatchedEvents = [];

        _accountRepositoryMock
            .Setup(x => x.GetByEmailAsync(emailAddress, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ILocalEvent>, CancellationToken>((events, _) => dispatchedEvents.AddRange(events.OfType<IDomainEvent>()))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(new ConfirmAuthEmailCommand { EmailAddress = "flower@example.com" }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        account.IsEmailConfirmed.Should().BeTrue();
        dispatchedEvents.Should().ContainSingle(x => x is AccountConfirmedDomainEvent);
    }

    [Fact]
    public async Task Handle_WhenAccountDoesNotExist_ReturnsNotFound()
    {
        var emailAddress = EmailAddress.Create("flower@example.com");

        _accountRepositoryMock
            .Setup(x => x.GetByEmailAsync(emailAddress, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var result = await _handler.Handle(new ConfirmAuthEmailCommand { EmailAddress = "flower@example.com" }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenEmailIsAlreadyConfirmed_ReturnsSuccessWithoutUpdatingOrDispatchingEvent()
    {
        var emailAddress = EmailAddress.Create("flower@example.com");
        var account = Account.Restore(Guid.NewGuid(), "flower", emailAddress, "hash", "stamp", 0, true);

        _accountRepositoryMock
            .Setup(x => x.GetByEmailAsync(emailAddress, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var result = await _handler.Handle(
            new ConfirmAuthEmailCommand { EmailAddress = "flower@example.com" },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _accountRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _domainEventDispatcherMock.Verify(
            x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

}

