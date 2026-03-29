using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.AuthService.Application.Features.Users.Commands.LoginUser;
using FlowChat.AuthService.Application.Features.Users.Models;
using FlowChat.AuthService.Domain.Entities;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;

namespace FlowChat.AuthService.UnitTests;

public sealed class LoginUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsAccessToken_WhenCredentialsAreValid()
    {
        var expiresAtUtc = DateTime.UtcNow.AddHours(1);
        var authenticatedUser = new AuthenticatedUser
        {
            Id = Guid.NewGuid(),
            UserName = "flower",
            Email = "flower@example.com",
            Roles = ["User"]
        };
        var repository = new LoginUserIdentityRepository(authenticatedUser);
        var jwtTokenGenerator = new FakeJwtTokenGenerator(
            new JwtTokenResult
            {
                AccessToken = "jwt-token",
                ExpiresAtUtc = expiresAtUtc
            });
        var domainEventDispatcher = new CapturingDomainEventDispatcher();
        var unitOfWork = new PassThroughUnitOfWork();
        var handler = new LoginUserCommandHandler(
            repository,
            jwtTokenGenerator,
            domainEventDispatcher,
            unitOfWork);

        var result = await handler.Handle(
            new LoginUserCommand
            {
                Login = "flower@example.com",
                Password = "P@ssw0rd!"
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("jwt-token", result.Value.AccessToken);
        Assert.Equal(expiresAtUtc, result.Value.ExpiresAtUtc);
        Assert.Empty(domainEventDispatcher.DispatchedBatches);
    }

    [Fact]
    public async Task Handle_ReturnsUnauthorized_WhenCredentialsAreInvalid()
    {
        var repository = new LoginUserIdentityRepository(null);
        var jwtTokenGenerator = new FakeJwtTokenGenerator(
            new JwtTokenResult
            {
                AccessToken = "unused",
                ExpiresAtUtc = DateTime.UtcNow.AddHours(1)
            });
        var domainEventDispatcher = new CapturingDomainEventDispatcher();
        var unitOfWork = new PassThroughUnitOfWork();
        var handler = new LoginUserCommandHandler(
            repository,
            jwtTokenGenerator,
            domainEventDispatcher,
            unitOfWork);

        var result = await handler.Handle(
            new LoginUserCommand
            {
                Login = "flower@example.com",
                Password = "wrong-password"
            },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, result.Error.ErrorType);
        Assert.Equal("Invalid credentials.", result.Error.ErrorMessage);
        Assert.False(jwtTokenGenerator.WasCalled);
        Assert.Empty(domainEventDispatcher.DispatchedBatches);
    }

    private sealed class LoginUserIdentityRepository : IIdentityRepository
    {
        private readonly AuthenticatedUser? _authenticatedUser;

        public LoginUserIdentityRepository(AuthenticatedUser? authenticatedUser)
        {
            _authenticatedUser = authenticatedUser;
        }

        public Task<Guid> CreateUserAsync(Identity user, string password, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<Identity?> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task UpdateAsync(Identity user, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<AuthenticatedUser?> AuthenticateUserAsync(string login, string password, CancellationToken cancellationToken)
            => Task.FromResult(_authenticatedUser);
    }

    private sealed class FakeJwtTokenGenerator : IJwtTokenGenerator
    {
        private readonly JwtTokenResult _tokenResult;

        public FakeJwtTokenGenerator(JwtTokenResult tokenResult)
        {
            _tokenResult = tokenResult;
        }

        public bool WasCalled { get; private set; }

        public JwtTokenResult GenerateToken(AuthenticatedUser user)
        {
            WasCalled = true;
            return _tokenResult;
        }
    }

    private sealed class PassThroughUnitOfWork : IUnitOfWork
    {
        public void Dispose()
        {
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
            => operation(cancellationToken);
    }

    private sealed class CapturingDomainEventDispatcher : IDomainEventDispatcher
    {
        public List<IReadOnlyCollection<IDomainEvent>> DispatchedBatches { get; } = [];

        public Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
        {
            DispatchedBatches.Add(domainEvents.ToArray());
            return Task.CompletedTask;
        }
    }
}
