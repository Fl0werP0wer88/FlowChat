using AutoFixture;
using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.AuthService.Application.Features.Users.Commands.RefreshToken;
using FlowChat.AuthService.Application.Features.Users.Models;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FluentAssertions;
using Moq;

namespace FlowChat.AuthService.UnitTests.Application.Users.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IIdentityRepository> _identityRepositoryMock = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGeneratorMock = new();
    private readonly Mock<IDomainEventDispatcher> _domainEventDispatcherMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly RefreshTokenCommandHandler _handler;

    public RefreshTokenCommandHandlerTests()
    {
        _unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<RefreshTokenCommandResponse>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<RefreshTokenCommandResponse>>>, CancellationToken>(
                (operation, ct) => operation(ct));

        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new RefreshTokenCommandHandler(
            _identityRepositoryMock.Object,
            _jwtTokenGeneratorMock.Object,
            _domainEventDispatcherMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_ValidTokens_ReturnsNewTokenPair()
    {
        var userId = _fixture.Create<Guid>();
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        var authenticatedUser = new AuthenticatedUser
        {
            Id = userId,
            UserName = "flower",
            Email = "flower@example.com",
            Roles = ["User"]
        };

        _jwtTokenGeneratorMock
            .Setup(x => x.ExtractUserIdFromExpiredToken("expired-access-token"))
            .Returns(userId);

        _identityRepositoryMock
            .Setup(x => x.GetRefreshTokenAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(("old-refresh-token", refreshTokenExpiry));

        _identityRepositoryMock
            .Setup(x => x.GetAuthenticatedUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(authenticatedUser);

        var newExpiresAtUtc = _fixture.Create<DateTime>().ToUniversalTime();
        _jwtTokenGeneratorMock
            .Setup(x => x.GenerateToken(authenticatedUser))
            .Returns(new JwtTokenResult
            {
                AccessToken = "new-access-token",
                ExpiresAtUtc = newExpiresAtUtc,
                RefreshToken = "new-refresh-token",
                RefreshTokenExpiresAtUtc = newExpiresAtUtc.AddDays(7)
            });

        var result = await _handler.Handle(
            new RefreshTokenCommand
            {
                AccessToken = "expired-access-token",
                RefreshToken = "old-refresh-token"
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("new-access-token");
        result.Value.ExpiresAtUtc.Should().Be(newExpiresAtUtc);
        result.Value.RefreshToken.Should().Be("new-refresh-token");
        result.Value.RefreshTokenExpiresAtUtc.Should().Be(newExpiresAtUtc.AddDays(7));

        _identityRepositoryMock.Verify(
            x => x.RevokeRefreshTokenAsync(userId, It.IsAny<CancellationToken>()),
            Times.Once);
        _identityRepositoryMock.Verify(
            x => x.SaveRefreshTokenAsync(userId, "new-refresh-token", newExpiresAtUtc.AddDays(7), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidAccessToken_ReturnsUnauthorized()
    {
        _jwtTokenGeneratorMock
            .Setup(x => x.ExtractUserIdFromExpiredToken("garbage-token"))
            .Returns((Guid?)null);

        var result = await _handler.Handle(
            new RefreshTokenCommand
            {
                AccessToken = "garbage-token",
                RefreshToken = "any-refresh-token"
            },
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Unauthorized);
        result.Error.ErrorMessage.Should().Be("Invalid access token.");
        _identityRepositoryMock.Verify(
            x => x.GetRefreshTokenAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_NoStoredRefreshToken_ReturnsUnauthorized()
    {
        var userId = _fixture.Create<Guid>();

        _jwtTokenGeneratorMock
            .Setup(x => x.ExtractUserIdFromExpiredToken("valid-access-token"))
            .Returns(userId);

        _identityRepositoryMock
            .Setup(x => x.GetRefreshTokenAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((string Token, DateTime ExpiresAtUtc)?)null);

        var result = await _handler.Handle(
            new RefreshTokenCommand
            {
                AccessToken = "valid-access-token",
                RefreshToken = "some-refresh-token"
            },
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Unauthorized);
        result.Error.ErrorMessage.Should().Be("Invalid or expired refresh token.");
    }

    [Fact]
    public async Task Handle_RefreshTokenMismatch_ReturnsUnauthorized()
    {
        var userId = _fixture.Create<Guid>();

        _jwtTokenGeneratorMock
            .Setup(x => x.ExtractUserIdFromExpiredToken("valid-access-token"))
            .Returns(userId);

        _identityRepositoryMock
            .Setup(x => x.GetRefreshTokenAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(("stored-refresh-token", DateTime.UtcNow.AddDays(7)));

        var result = await _handler.Handle(
            new RefreshTokenCommand
            {
                AccessToken = "valid-access-token",
                RefreshToken = "wrong-refresh-token"
            },
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Unauthorized);
        result.Error.ErrorMessage.Should().Be("Invalid or expired refresh token.");
    }

    [Fact]
    public async Task Handle_ExpiredRefreshToken_ReturnsUnauthorized()
    {
        var userId = _fixture.Create<Guid>();

        _jwtTokenGeneratorMock
            .Setup(x => x.ExtractUserIdFromExpiredToken("valid-access-token"))
            .Returns(userId);

        _identityRepositoryMock
            .Setup(x => x.GetRefreshTokenAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(("the-refresh-token", DateTime.UtcNow.AddMinutes(-1)));

        var result = await _handler.Handle(
            new RefreshTokenCommand
            {
                AccessToken = "valid-access-token",
                RefreshToken = "the-refresh-token"
            },
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Unauthorized);
        result.Error.ErrorMessage.Should().Be("Invalid or expired refresh token.");
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsUnauthorized()
    {
        var userId = _fixture.Create<Guid>();

        _jwtTokenGeneratorMock
            .Setup(x => x.ExtractUserIdFromExpiredToken("valid-access-token"))
            .Returns(userId);

        _identityRepositoryMock
            .Setup(x => x.GetRefreshTokenAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(("valid-refresh-token", DateTime.UtcNow.AddDays(7)));

        _identityRepositoryMock
            .Setup(x => x.GetAuthenticatedUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthenticatedUser?)null);

        var result = await _handler.Handle(
            new RefreshTokenCommand
            {
                AccessToken = "valid-access-token",
                RefreshToken = "valid-refresh-token"
            },
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Unauthorized);
        result.Error.ErrorMessage.Should().Be("User not found or account is not confirmed.");
    }
}
