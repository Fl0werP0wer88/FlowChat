using FlowChat.Application.Abstractions;
using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.AuthService.Application.Features.Users.Commands.ConfirmUserEmail;
using FlowChat.AuthService.Application.Features.Users.Models;
using FlowChat.AuthService.Domain.Entities;
using FlowChat.AuthService.Domain.Events;
using FlowChat.Domain.Abstractions;

namespace FlowChat.AuthService.UnitTests;

public sealed class ConfirmUserEmailCommandHandlerTests
{
    [Fact]
    public async Task Handle_LoadsAggregate_ConfirmsEmail_AndDispatchesDomainEvents()
    {
        var existingUser = Identity.Restore(
            Guid.NewGuid(),
            "flower",
            "flower@example.com",
            "+48123123123",
            emailConfirmed: false,
            phoneNumberConfirmed: false);
        var repository = new ConfirmUserIdentityRepository(existingUser, isTokenValid: true);
        var tokenEncoder = new ConfirmUserTokenEncoder("decoded-token");
        var domainEventDispatcher = new CapturingDomainEventDispatcher();
        var unitOfWork = new PassThroughUnitOfWork();
        var handler = new ConfirmUserEmailCommandHandler(
            repository,
            tokenEncoder,
            domainEventDispatcher,
            unitOfWork);

        var result = await handler.Handle(
            new ConfirmUserEmailCommand
            {
                UserId = existingUser.Id.Value,
                Token = "encoded-token"
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(repository.UpdatedUser?.EmailConfirmed);
        Assert.True(repository.UpdatedUser?.AccountConfirmed);

        var dispatchedEvents = Assert.Single(domainEventDispatcher.DispatchedBatches);
        Assert.Collection(
            dispatchedEvents,
            domainEvent => Assert.IsType<EmailConfirmedDomainEvent>(domainEvent),
            domainEvent => Assert.IsType<AccountConfirmedDomainEvent>(domainEvent));
    }

    [Fact]
    public async Task Handle_ReturnsNotFound_WhenUserDoesNotExist()
    {
        var repository = new ConfirmUserIdentityRepository(null, isTokenValid: true);
        var tokenEncoder = new ConfirmUserTokenEncoder("decoded-token");
        var domainEventDispatcher = new CapturingDomainEventDispatcher();
        var unitOfWork = new PassThroughUnitOfWork();
        var handler = new ConfirmUserEmailCommandHandler(
            repository,
            tokenEncoder,
            domainEventDispatcher,
            unitOfWork);

        var result = await handler.Handle(
            new ConfirmUserEmailCommand
            {
                UserId = Guid.NewGuid(),
                Token = "encoded-token"
            },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.Error.ErrorType);
        Assert.Equal("User was not found.", result.Error.ErrorMessage);
        Assert.Null(repository.UpdatedUser);
        Assert.Empty(domainEventDispatcher.DispatchedBatches);
    }

    private sealed class ConfirmUserIdentityRepository : IIdentityRepository
    {
        private readonly Identity? _loadedUser;
        private readonly bool _isTokenValid;

        public ConfirmUserIdentityRepository(Identity? loadedUser, bool isTokenValid)
        {
            _loadedUser = loadedUser;
            _isTokenValid = isTokenValid;
        }

        public Identity? UpdatedUser { get; private set; }

        public Task<Guid> CreateUserAsync(Identity user, string password, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<Identity?> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult(_loadedUser);

        public Task<string> GenerateEmailConfirmationTokenAsync(Guid userId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<bool> IsEmailConfirmationTokenValidAsync(Guid userId, string token, CancellationToken cancellationToken)
            => Task.FromResult(_isTokenValid);

        public Task UpdateAsync(Identity user, CancellationToken cancellationToken)
        {
            UpdatedUser = user;
            return Task.CompletedTask;
        }

        public Task<AuthenticatedUser?> AuthenticateUserAsync(string login, string password, CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }

    private sealed class ConfirmUserTokenEncoder : ITokenEncoder
    {
        private readonly string _decodedToken;

        public ConfirmUserTokenEncoder(string decodedToken)
        {
            _decodedToken = decodedToken;
        }

        public string EncodeForUrl(string token) => throw new NotSupportedException();

        public string DecodeFromUrl(string encodedToken) => _decodedToken;
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
