using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.AuthService.Application.Features.Users.Commands.ConfirmAuthEmail;
using FlowChat.AuthService.Application.Features.Users.Models;
using FlowChat.AuthService.Domain.Entities;
using FlowChat.AuthService.Domain.Events;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using MediatR;

namespace FlowChat.AuthService.UnitTests;

public sealed class ConfirmAuthEmailCommandHandlerTests
{
    [Fact]
    public async Task Handle_ConfirmsEmail_UpdatesUser_AndDispatchesAccountConfirmedDomainEvent()
    {
        var user = Identity.Restore(
            Guid.NewGuid(),
            "flower",
            "flower@example.com",
            null,
            emailConfirmed: false,
            phoneNumberConfirmed: false);
        var repository = new ConfirmAuthEmailIdentityRepository(user);
        var unitOfWork = new PassThroughUnitOfWork();
        var domainEventDispatcher = new CapturingDomainEventDispatcher();
        var handler = new ConfirmAuthEmailCommandHandler(repository, domainEventDispatcher, unitOfWork);

        var result = await handler.Handle(
            new ConfirmAuthEmailCommand
            {
                EmailAddress = "flower@example.com"
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.UpdatedUser);
        Assert.True(repository.UpdatedUser!.EmailConfirmed);

        var dispatchedEvents = Assert.Single(domainEventDispatcher.DispatchedBatches);
        Assert.IsType<AccountConfirmedDomainEvent>(Assert.Single(dispatchedEvents));
    }

    [Fact]
    public async Task Handle_WhenUserIsAlreadyConfirmed_ReturnsConflictWithoutUpdate()
    {
        var user = Identity.Restore(
            Guid.NewGuid(),
            "flower",
            "flower@example.com",
            null,
            emailConfirmed: true,
            phoneNumberConfirmed: false);
        var repository = new ConfirmAuthEmailIdentityRepository(user);
        var unitOfWork = new PassThroughUnitOfWork();
        var domainEventDispatcher = new CapturingDomainEventDispatcher();
        var handler = new ConfirmAuthEmailCommandHandler(repository, domainEventDispatcher, unitOfWork);

        var result = await handler.Handle(
            new ConfirmAuthEmailCommand
            {
                EmailAddress = "flower@example.com"
            },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.Error.ErrorType);
        Assert.Equal("Email was already confirmed.", result.Error.ErrorMessage);
        Assert.Null(repository.UpdatedUser);
        Assert.Empty(domainEventDispatcher.DispatchedBatches);
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ReturnsNotFound()
    {
        var repository = new ConfirmAuthEmailIdentityRepository(null);
        var unitOfWork = new PassThroughUnitOfWork();
        var domainEventDispatcher = new CapturingDomainEventDispatcher();
        var handler = new ConfirmAuthEmailCommandHandler(repository, domainEventDispatcher, unitOfWork);

        var result = await handler.Handle(
            new ConfirmAuthEmailCommand
            {
                EmailAddress = "flower@example.com"
            },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.Error.ErrorType);
        Assert.Equal("User was not found.", result.Error.ErrorMessage);
        Assert.Null(repository.UpdatedUser);
        Assert.Empty(domainEventDispatcher.DispatchedBatches);
    }

    private sealed class ConfirmAuthEmailIdentityRepository : IIdentityRepository
    {
        private readonly Identity? _loadedUser;

        public ConfirmAuthEmailIdentityRepository(Identity? loadedUser)
        {
            _loadedUser = loadedUser;
        }

        public Identity? UpdatedUser { get; private set; }

        public Task<Guid> CreateUserAsync(Identity user, string password, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<Identity?> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<Identity?> GetByEmailAsync(string emailAddress, CancellationToken cancellationToken)
            => Task.FromResult(_loadedUser);

        public Task UpdateAsync(Identity user, CancellationToken cancellationToken)
        {
            UpdatedUser = user;
            return Task.CompletedTask;
        }

        public Task<AuthenticatedUser?> AuthenticateUserAsync(string login, string password, CancellationToken cancellationToken)
            => throw new NotSupportedException();
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
