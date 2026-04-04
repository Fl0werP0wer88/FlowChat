using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.AuthService.Application.Features.User.Commands.ConfirmAuthEmail;
using FlowChat.AuthService.Domain.Entities.Account;
using FlowChat.AuthService.Domain.Entities.Account.Events;
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
    private readonly Mock<IDomainEventDispatcher> _domainEventDispatcherMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly ConfirmAuthEmailCommandHandler _handler;

    public ConfirmAuthEmailCommandHandlerTests()
    {
        _unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<Unit>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<Unit>>>, CancellationToken>((operation, ct) => operation(ct));

        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new ConfirmAuthEmailCommandHandler(
            _accountRepositoryMock.Object,
            _domainEventDispatcherMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_WhenAccountExists_ConfirmsEmailAndDispatchesAccountConfirmedDomainEvent()
    {
        var account = Account.Restore(Guid.NewGuid(), "flower", EmailAddress.Create("flower@example.com"), "hash", "stamp", 0, false);
        List<IDomainEvent> dispatchedEvents = [];

        _accountRepositoryMock
            .Setup(x => x.GetByEmailAsync("flower@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<IDomainEvent>, CancellationToken>((events, _) => dispatchedEvents.AddRange(events))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(new ConfirmAuthEmailCommand { EmailAddress = "flower@example.com" }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        account.IsEmailConfirmed.Should().BeTrue();
        dispatchedEvents.Should().ContainSingle(x => x is AccountConfirmedDomainEvent);
    }

    [Fact]
    public async Task Handle_WhenAccountDoesNotExist_ReturnsNotFound()
    {
        _accountRepositoryMock
            .Setup(x => x.GetByEmailAsync("flower@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var result = await _handler.Handle(new ConfirmAuthEmailCommand { EmailAddress = "flower@example.com" }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);
    }
}
