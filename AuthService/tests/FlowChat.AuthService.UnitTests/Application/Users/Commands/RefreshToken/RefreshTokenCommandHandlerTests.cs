using System.Security.Claims;
using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.AuthService.Application.Features.User.Commands.RefreshToken;
using FlowChat.AuthService.Application.Features.User.Models;
using FlowChat.AuthService.Domain.Entities.Account;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace FlowChat.AuthService.UnitTests;

public sealed class RefreshTokenCommandHandlerTests
{
    private readonly Mock<IAccountRepository> _accountRepositoryMock = new();
    private readonly Mock<IOpenIddictTokenService> _openIddictTokenServiceMock = new();
    private readonly Mock<IDomainEventDispatcher> _domainEventDispatcherMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly RefreshTokenCommandHandler _handler;

    public RefreshTokenCommandHandlerTests()
    {
        _unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<RefreshTokenCommandResponse>>>>(),
                It.IsAny<Func<FlowChatResult<RefreshTokenCommandResponse>, CancellationToken, Task<FlowChatResult<RefreshTokenCommandResponse>>>>(),
                It.IsAny<Func<Exception, CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns<
                Func<CancellationToken, Task<FlowChatResult<RefreshTokenCommandResponse>>>,
                Func<FlowChatResult<RefreshTokenCommandResponse>, CancellationToken, Task<FlowChatResult<RefreshTokenCommandResponse>>>,
                Func<Exception, CancellationToken, Task>,
                CancellationToken>(async (operation, beforeCommitOperation, _, ct) =>
                {
                    var result = await operation(ct);
                    return await beforeCommitOperation(result, ct);
                });

        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new RefreshTokenCommandHandler(
            _accountRepositoryMock.Object,
            _openIddictTokenServiceMock.Object,
            _domainEventDispatcherMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_WhenAccountExistsAndIsConfirmed_ReturnsPrincipal()
    {
        var account = Account.Restore(
            Guid.NewGuid(),
            "flower",
            EmailAddress.Create("flower@example.com"),
            "hashed-password",
            "stamp",
            0,
            true);
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, account.Id.Value.ToString())]));

        _accountRepositoryMock
            .Setup(x => x.GetByIdAsync(account.Id.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        _openIddictTokenServiceMock
            .Setup(x => x.CreatePrincipal(It.IsAny<AuthenticatedAccount>(), It.IsAny<IEnumerable<string>>()))
            .Returns(principal);

        var result = await _handler.Handle(
            new RefreshTokenCommand
            {
                AccountId = account.Id.Value,
                Scopes = ["offline_access"]
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Grant.Principal.Should().BeSameAs(principal);
    }

    [Fact]
    public async Task Handle_WhenAccountIsMissing_ReturnsUnauthorized()
    {
        _accountRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Id<Account>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var result = await _handler.Handle(
            new RefreshTokenCommand
            {
                AccountId = Guid.NewGuid(),
                Scopes = ["offline_access"]
            },
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Unauthorized);
    }
}

