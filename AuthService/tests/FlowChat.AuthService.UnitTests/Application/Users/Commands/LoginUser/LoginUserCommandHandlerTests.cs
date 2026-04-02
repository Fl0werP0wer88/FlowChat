using AutoFixture;
using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.AuthService.Application.Features.User.Commands.LoginUser;
using FlowChat.AuthService.Application.Features.User.Models;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FluentAssertions;
using Moq;

namespace FlowChat.AuthService.UnitTests;

public sealed class LoginUserCommandHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IIdentityRepository> _identityRepositoryMock = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGeneratorMock = new();
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
            _identityRepositoryMock.Object,
            _jwtTokenGeneratorMock.Object,
            _domainEventDispatcherMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_WhenCredentialsAreValid_ReturnsAccessToken()
    {
        var expiresAtUtc = _fixture.Create<DateTime>().ToUniversalTime();
        var refreshTokenExpiresAtUtc = expiresAtUtc.AddDays(7);
        var authenticatedUser = new AuthenticatedUser
        {
            Id = _fixture.Create<Guid>(),
            UserName = "flower",
            Email = "flower@example.com",
            Roles = ["User"]
        };
        var refreshToken = new RefreshTokenResult
        {
            Token = "refresh-token",
            ExpiresAtUtc = refreshTokenExpiresAtUtc
        };

        _identityRepositoryMock
            .Setup(x => x.LoginUserAsync(
                "flower@example.com",
                "P@ssw0rd!",
                refreshToken.Token,
                refreshToken.ExpiresAtUtc,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(authenticatedUser);

        _jwtTokenGeneratorMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns(refreshToken);

        _jwtTokenGeneratorMock
            .Setup(x => x.GenerateToken(authenticatedUser, refreshToken.Token, refreshToken.ExpiresAtUtc))
            .Returns(new JwtTokenResult
            {
                AccessToken = "jwt-token",
                ExpiresAtUtc = expiresAtUtc,
                RefreshToken = refreshToken.Token,
                RefreshTokenExpiresAtUtc = refreshToken.ExpiresAtUtc
            });

        var result = await _handler.Handle(
            new LoginUserCommand
            {
                Login = "flower@example.com",
                Password = "P@ssw0rd!"
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("jwt-token");
        result.Value.ExpiresAtUtc.Should().Be(expiresAtUtc);
        result.Value.RefreshToken.Should().Be(refreshToken.Token);
        result.Value.RefreshTokenExpiresAtUtc.Should().Be(refreshToken.ExpiresAtUtc);
        _identityRepositoryMock.Verify(
            x => x.LoginUserAsync(
                "flower@example.com",
                "P@ssw0rd!",
                refreshToken.Token,
                refreshToken.ExpiresAtUtc,
                It.IsAny<CancellationToken>()),
            Times.Once);
        _domainEventDispatcherMock.Verify(
            x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCredentialsAreInvalid_ReturnsUnauthorizedFailure()
    {
        var refreshToken = new RefreshTokenResult
        {
            Token = "refresh-token",
            ExpiresAtUtc = _fixture.Create<DateTime>().ToUniversalTime()
        };

        _jwtTokenGeneratorMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns(refreshToken);

        _identityRepositoryMock
            .Setup(x => x.LoginUserAsync(
                "flower@example.com",
                "wrong-password",
                refreshToken.Token,
                refreshToken.ExpiresAtUtc,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthenticatedUser?)null);

        var result = await _handler.Handle(
            new LoginUserCommand
            {
                Login = "flower@example.com",
                Password = "wrong-password"
            },
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Unauthorized);
        result.Error.ErrorMessage.Should().Be("Invalid credentials or account is not confirmed.");
        _jwtTokenGeneratorMock.Verify(x => x.GenerateToken(It.IsAny<AuthenticatedUser>()), Times.Never);
        _jwtTokenGeneratorMock.Verify(
            x => x.GenerateToken(It.IsAny<AuthenticatedUser>(), It.IsAny<string>(), It.IsAny<DateTime>()),
            Times.Never);
        _domainEventDispatcherMock.Verify(
            x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
