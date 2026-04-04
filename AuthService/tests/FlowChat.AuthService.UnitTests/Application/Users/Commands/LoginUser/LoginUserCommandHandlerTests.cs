using System.Security.Claims;
using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.AuthService.Application.Features.User.Commands.LoginUser;
using FlowChat.AuthService.Application.Features.User.Models;
using FlowChat.AuthService.Domain.Entities.Account;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace FlowChat.AuthService.UnitTests;

public sealed class LoginUserCommandHandlerTests
{
    private readonly Mock<IAccountRepository> _accountRepositoryMock = new();
    private readonly Mock<IPasswordHashingService> _passwordHashingServiceMock = new();
    private readonly Mock<IOpenIddictTokenService> _openIddictTokenServiceMock = new();
    private readonly Mock<IDomainEventDispatcher> _domainEventDispatcherMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly LoginUserCommandHandler _handler;

    public LoginUserCommandHandlerTests()
    {
        _unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<LoginUserCommandResponse>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<LoginUserCommandResponse>>>, CancellationToken>((operation, ct) => operation(ct));

        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new LoginUserCommandHandler(
            _accountRepositoryMock.Object,
            _passwordHashingServiceMock.Object,
            _openIddictTokenServiceMock.Object,
            _domainEventDispatcherMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_WhenCredentialsAreValid_ReturnsPrincipalAndResetsFailedCount()
    {
        var account = Account.Restore(
            Guid.NewGuid(),
            "flower",
            EmailAddress.Create("flower@example.com"),
            "hashed-password",
            "stamp",
            2,
            true);
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, account.Id.Value.ToString())]));

        _accountRepositoryMock
            .Setup(x => x.GetByLoginAsync("flower@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        _passwordHashingServiceMock
            .Setup(x => x.VerifyHashedPassword("hashed-password", "P@ssw0rd!"))
            .Returns(PasswordVerificationResult.Succeeded);
        _openIddictTokenServiceMock
            .Setup(x => x.CreatePrincipal(It.IsAny<AuthenticatedAccount>(), It.IsAny<IEnumerable<string>>()))
            .Returns(principal);

        var result = await _handler.Handle(
            new LoginUserCommand
            {
                Login = "flower@example.com",
                Password = "P@ssw0rd!",
                Scopes = ["offline_access"]
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Grant.Principal.Should().BeSameAs(principal);
        account.AccessFailedCount.Should().Be(0);
        _accountRepositoryMock.Verify(x => x.UpdateAsync(account, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPasswordIsInvalid_ReturnsUnauthorizedAndIncrementsFailedCount()
    {
        var account = Account.Restore(
            Guid.NewGuid(),
            "flower",
            EmailAddress.Create("flower@example.com"),
            "hashed-password",
            "stamp",
            0,
            true);

        _accountRepositoryMock
            .Setup(x => x.GetByLoginAsync("flower@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        _passwordHashingServiceMock
            .Setup(x => x.VerifyHashedPassword("hashed-password", "wrong-password"))
            .Returns(PasswordVerificationResult.Failed);

        var result = await _handler.Handle(
            new LoginUserCommand
            {
                Login = "flower@example.com",
                Password = "wrong-password"
            },
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Unauthorized);
        account.AccessFailedCount.Should().Be(1);
        _accountRepositoryMock.Verify(x => x.UpdateAsync(account, It.IsAny<CancellationToken>()), Times.Once);
        _openIddictTokenServiceMock.Verify(x => x.CreatePrincipal(It.IsAny<AuthenticatedAccount>(), It.IsAny<IEnumerable<string>>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAccountIsMissingOrUnconfirmed_ReturnsUnauthorized()
    {
        _accountRepositoryMock
            .Setup(x => x.GetByLoginAsync("flower@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var result = await _handler.Handle(
            new LoginUserCommand
            {
                Login = "flower@example.com",
                Password = "P@ssw0rd!"
            },
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Unauthorized);
        _accountRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
