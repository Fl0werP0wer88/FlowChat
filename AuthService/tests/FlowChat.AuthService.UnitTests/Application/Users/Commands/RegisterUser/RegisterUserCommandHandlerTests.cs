using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.AuthService.Application.Features.Users.Commands.RegisterUser;
using FlowChat.AuthService.Application.Features.Users.Models;
using FlowChat.AuthService.Domain.Entities;
using FlowChat.AuthService.Domain.Events;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;

namespace FlowChat.AuthService.UnitTests;

public sealed class RegisterUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_CreatesUser_AndDispatchesAccountRegisteredDomainEvent()
    {
        var repository = new FakeIdentityRepository();
        var unitOfWork = new TrackingUnitOfWork();
        var domainEventDispatcher = new CapturingDomainEventDispatcher();
        var handler = CreateHandler(repository, unitOfWork, domainEventDispatcher);

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);
        var createdUser = repository.CreatedUser;
        Assert.NotNull(createdUser);
        var createdUserId = createdUser.Id.Value;

        Assert.True(result.IsSuccess);
        Assert.Equal(createdUserId, result.Value.Id);
        Assert.False(createdUser.EmailConfirmed);
        Assert.False(createdUser.AccountConfirmed);

        var dispatchedEvents = Assert.Single(domainEventDispatcher.DispatchedBatches);
        var accountRegisteredDomainEvent = Assert.IsType<AccountRegisteredDomainEvent>(Assert.Single(dispatchedEvents));
        Assert.Equal(createdUserId, accountRegisteredDomainEvent.UserId.Value);
    }

    [Fact]
    public async Task Handle_ReturnsConflict_WhenDuplicateUserNameIsReportedByRepository()
    {
        var repository = new FakeIdentityRepository(
            createUserException: new InvalidOperationException(
                "User creation failed: DuplicateUserName: Username 'flower' is already taken."));
        var unitOfWork = new TrackingUnitOfWork();
        var domainEventDispatcher = new CapturingDomainEventDispatcher();
        var handler = CreateHandler(repository, unitOfWork, domainEventDispatcher);

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.Error.ErrorType);
        Assert.Equal("User with the provided username or email already exists.", result.Error.ErrorMessage);
        Assert.Contains("Username 'flower' is already taken.", result.Error.Errors ?? []);
        Assert.Empty(domainEventDispatcher.DispatchedBatches);
    }

    [Fact]
    public async Task Handle_ReturnsConflict_WhenDuplicateEmailIsReportedByRepository()
    {
        var repository = new FakeIdentityRepository(
            createUserException: new InvalidOperationException(
                "User creation failed: DuplicateEmail: Email 'flower@example.com' is already taken."));
        var unitOfWork = new TrackingUnitOfWork();
        var domainEventDispatcher = new CapturingDomainEventDispatcher();
        var handler = CreateHandler(repository, unitOfWork, domainEventDispatcher);

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.Error.ErrorType);
        Assert.Equal("User with the provided username or email already exists.", result.Error.ErrorMessage);
        Assert.Contains("Email 'flower@example.com' is already taken.", result.Error.Errors ?? []);
        Assert.Empty(domainEventDispatcher.DispatchedBatches);
    }

    [Fact]
    public async Task Handle_ReturnsValidation_WhenRepositoryReportsValidationErrors()
    {
        var repository = new FakeIdentityRepository(
            createUserException: new InvalidOperationException(
                "User creation failed: PasswordTooShort: Password must be at least 8 characters.; PasswordRequiresUpper: Password must contain an uppercase letter."));
        var unitOfWork = new TrackingUnitOfWork();
        var domainEventDispatcher = new CapturingDomainEventDispatcher();
        var handler = CreateHandler(repository, unitOfWork, domainEventDispatcher);

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
        Assert.Equal("User registration validation failed.", result.Error.ErrorMessage);
        Assert.Contains("Password must be at least 8 characters.", result.Error.Errors ?? []);
        Assert.Contains("Password must contain an uppercase letter.", result.Error.Errors ?? []);
        Assert.Empty(domainEventDispatcher.DispatchedBatches);
    }

    [Fact]
    public async Task Handle_RethrowsUnexpectedInvalidOperationException()
    {
        var repository = new FakeIdentityRepository(
            createUserException: new InvalidOperationException("Database connection failed."));
        var unitOfWork = new TrackingUnitOfWork();
        var domainEventDispatcher = new CapturingDomainEventDispatcher();
        var handler = CreateHandler(repository, unitOfWork, domainEventDispatcher);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(CreateCommand(), CancellationToken.None));

        Assert.Equal("Database connection failed.", exception.Message);
        Assert.Empty(domainEventDispatcher.DispatchedBatches);
    }

    private static RegisterUserCommandHandler CreateHandler(
        FakeIdentityRepository repository,
        TrackingUnitOfWork unitOfWork,
        CapturingDomainEventDispatcher domainEventDispatcher)
    {
        return new RegisterUserCommandHandler(
            repository,
            unitOfWork,
            domainEventDispatcher);
    }

    private static RegisterUserCommand CreateCommand()
    {
        return new RegisterUserCommand
        {
            UserName = "flower",
            Email = "flower@example.com",
            PhoneNumber = "+48123123123",
            Password = "P@ssw0rd!"
        };
    }

    private sealed class FakeIdentityRepository : IIdentityRepository
    {
        private readonly Exception? _createUserException;

        public FakeIdentityRepository(Exception? createUserException = null)
        {
            _createUserException = createUserException;
        }

        public Identity? CreatedUser { get; private set; }

        public Task<Guid> CreateUserAsync(Identity user, string password, CancellationToken cancellationToken)
        {
            if (_createUserException is not null)
            {
                throw _createUserException;
            }

            CreatedUser = user;
            return Task.FromResult(user.Id.Value);
        }

        public Task<Identity?> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<Identity?> GetByEmailAsync(string emailAddress, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task UpdateAsync(Identity user, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<AuthenticatedUser?> AuthenticateUserAsync(string login, string password, CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }

    private sealed class TrackingUnitOfWork : IUnitOfWork
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
