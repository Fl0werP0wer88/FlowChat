using FlowChat.Shared.Application;
using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.AuthService.Application.Features.Users.Commands.RegisterUser;
using FlowChat.AuthService.Application.Features.Users.Models;
using FlowChat.AuthService.Domain.Entities;
using FlowChat.AuthService.Domain.Events;
using FlowChat.Shared.Domain;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.AuthService.Events;

namespace FlowChat.AuthService.UnitTests;

public sealed class RegisterUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_PublishesEmailVerificationIntegrationEvent_InsideTransaction()
    {
        var repository = new FakeIdentityRepository(confirmationToken: "raw-confirmation-token");
        var tokenEncoder = new FakeTokenEncoder("encoded-confirmation-token");
        var confirmationLinkBuilder = new FakeConfirmationLinkBuilder("https://localhost/confirm?token=encoded-confirmation-token");
        var integrationEventPublisher = new CapturingIntegrationEventPublisher();
        var unitOfWork = new TrackingUnitOfWork(integrationEventPublisher);
        var domainEventDispatcher = new CapturingDomainEventDispatcher();
        var handler = CreateHandler(
            repository,
            tokenEncoder,
            confirmationLinkBuilder,
            integrationEventPublisher,
            unitOfWork,
            domainEventDispatcher);

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);
        var createdUser = repository.CreatedUser;
        Assert.NotNull(createdUser);
        var createdUserId = createdUser.Id.Value;

        Assert.True(result.IsSuccess);
        Assert.Equal(createdUserId, result.Value.Id);

        var publishedEvent = Assert.IsType<EmailVerificationRequestIntegrationEvent>(integrationEventPublisher.PublishedEvent);
        Assert.Equal(createdUserId.ToString(), publishedEvent.Key);
        Assert.Equal(createdUserId, publishedEvent.UserId);
        Assert.Equal("flower@example.com", publishedEvent.UserEmail);
        Assert.Equal("https://localhost/confirm?token=encoded-confirmation-token", publishedEvent.ConfirmationLink);
        Assert.True(integrationEventPublisher.PublishedInsideTransaction);
        Assert.Equal(1, repository.GenerateEmailConfirmationTokenCallCount);
        Assert.Equal(1, tokenEncoder.EncodeCallCount);
        Assert.Equal(1, confirmationLinkBuilder.BuildCallCount);

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
        var tokenEncoder = new FakeTokenEncoder("encoded-confirmation-token");
        var confirmationLinkBuilder = new FakeConfirmationLinkBuilder("https://localhost/confirm?token=encoded-confirmation-token");
        var integrationEventPublisher = new CapturingIntegrationEventPublisher();
        var unitOfWork = new TrackingUnitOfWork(integrationEventPublisher);
        var domainEventDispatcher = new CapturingDomainEventDispatcher();
        var handler = CreateHandler(
            repository,
            tokenEncoder,
            confirmationLinkBuilder,
            integrationEventPublisher,
            unitOfWork,
            domainEventDispatcher);

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.Error.ErrorType);
        Assert.Equal("User with the provided username or email already exists.", result.Error.ErrorMessage);
        Assert.Contains("Username 'flower' is already taken.", result.Error.Errors ?? []);
        Assert.Equal(0, repository.GenerateEmailConfirmationTokenCallCount);
        Assert.Equal(0, tokenEncoder.EncodeCallCount);
        Assert.Equal(0, confirmationLinkBuilder.BuildCallCount);
        Assert.Null(integrationEventPublisher.PublishedEvent);
        Assert.Empty(domainEventDispatcher.DispatchedBatches);
    }

    [Fact]
    public async Task Handle_ReturnsConflict_WhenDuplicateEmailIsReportedByRepository()
    {
        var repository = new FakeIdentityRepository(
            createUserException: new InvalidOperationException(
                "User creation failed: DuplicateEmail: Email 'flower@example.com' is already taken."));
        var tokenEncoder = new FakeTokenEncoder("encoded-confirmation-token");
        var confirmationLinkBuilder = new FakeConfirmationLinkBuilder("https://localhost/confirm?token=encoded-confirmation-token");
        var integrationEventPublisher = new CapturingIntegrationEventPublisher();
        var unitOfWork = new TrackingUnitOfWork(integrationEventPublisher);
        var domainEventDispatcher = new CapturingDomainEventDispatcher();
        var handler = CreateHandler(
            repository,
            tokenEncoder,
            confirmationLinkBuilder,
            integrationEventPublisher,
            unitOfWork,
            domainEventDispatcher);

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.Error.ErrorType);
        Assert.Equal("User with the provided username or email already exists.", result.Error.ErrorMessage);
        Assert.Contains("Email 'flower@example.com' is already taken.", result.Error.Errors ?? []);
        Assert.Equal(0, repository.GenerateEmailConfirmationTokenCallCount);
        Assert.Equal(0, tokenEncoder.EncodeCallCount);
        Assert.Equal(0, confirmationLinkBuilder.BuildCallCount);
        Assert.Null(integrationEventPublisher.PublishedEvent);
        Assert.Empty(domainEventDispatcher.DispatchedBatches);
    }

    [Fact]
    public async Task Handle_ReturnsValidation_WhenRepositoryReportsValidationErrors()
    {
        var repository = new FakeIdentityRepository(
            createUserException: new InvalidOperationException(
                "User creation failed: PasswordTooShort: Password must be at least 8 characters.; PasswordRequiresUpper: Password must contain an uppercase letter."));
        var tokenEncoder = new FakeTokenEncoder("encoded-confirmation-token");
        var confirmationLinkBuilder = new FakeConfirmationLinkBuilder("https://localhost/confirm?token=encoded-confirmation-token");
        var integrationEventPublisher = new CapturingIntegrationEventPublisher();
        var unitOfWork = new TrackingUnitOfWork(integrationEventPublisher);
        var domainEventDispatcher = new CapturingDomainEventDispatcher();
        var handler = CreateHandler(
            repository,
            tokenEncoder,
            confirmationLinkBuilder,
            integrationEventPublisher,
            unitOfWork,
            domainEventDispatcher);

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
        Assert.Equal("User registration validation failed.", result.Error.ErrorMessage);
        Assert.Contains("Password must be at least 8 characters.", result.Error.Errors ?? []);
        Assert.Contains("Password must contain an uppercase letter.", result.Error.Errors ?? []);
        Assert.Equal(0, repository.GenerateEmailConfirmationTokenCallCount);
        Assert.Equal(0, tokenEncoder.EncodeCallCount);
        Assert.Equal(0, confirmationLinkBuilder.BuildCallCount);
        Assert.Null(integrationEventPublisher.PublishedEvent);
        Assert.Empty(domainEventDispatcher.DispatchedBatches);
    }

    [Fact]
    public async Task Handle_RethrowsUnexpectedInvalidOperationException()
    {
        var repository = new FakeIdentityRepository(
            createUserException: new InvalidOperationException("Database connection failed."));
        var tokenEncoder = new FakeTokenEncoder("encoded-confirmation-token");
        var confirmationLinkBuilder = new FakeConfirmationLinkBuilder("https://localhost/confirm?token=encoded-confirmation-token");
        var integrationEventPublisher = new CapturingIntegrationEventPublisher();
        var unitOfWork = new TrackingUnitOfWork(integrationEventPublisher);
        var domainEventDispatcher = new CapturingDomainEventDispatcher();
        var handler = CreateHandler(
            repository,
            tokenEncoder,
            confirmationLinkBuilder,
            integrationEventPublisher,
            unitOfWork,
            domainEventDispatcher);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(CreateCommand(), CancellationToken.None));

        Assert.Equal("Database connection failed.", exception.Message);
        Assert.Equal(0, repository.GenerateEmailConfirmationTokenCallCount);
        Assert.Equal(0, tokenEncoder.EncodeCallCount);
        Assert.Equal(0, confirmationLinkBuilder.BuildCallCount);
        Assert.Null(integrationEventPublisher.PublishedEvent);
        Assert.Empty(domainEventDispatcher.DispatchedBatches);
    }

    private static RegisterUserCommandHandler CreateHandler(
        FakeIdentityRepository repository,
        FakeTokenEncoder tokenEncoder,
        FakeConfirmationLinkBuilder confirmationLinkBuilder,
        CapturingIntegrationEventPublisher integrationEventPublisher,
        TrackingUnitOfWork unitOfWork,
        CapturingDomainEventDispatcher domainEventDispatcher)
    {
        return new RegisterUserCommandHandler(
            repository,
            tokenEncoder,
            confirmationLinkBuilder,
            integrationEventPublisher,
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
        private readonly string _confirmationToken;
        private readonly Exception? _createUserException;

        public FakeIdentityRepository(string confirmationToken = "", Exception? createUserException = null)
        {
            _confirmationToken = confirmationToken;
            _createUserException = createUserException;
        }

        public Identity? CreatedUser { get; private set; }

        public int GenerateEmailConfirmationTokenCallCount { get; private set; }

        public Task<Guid> CreateUserAsync(Identity user, string password, CancellationToken cancellationToken)
        {
            if (_createUserException is not null)
            {
                throw _createUserException;
            }

            CreatedUser = user;
            return Task.FromResult(user.Id.Value);
        }

        public Task<string> GenerateEmailConfirmationTokenAsync(Guid userId, CancellationToken cancellationToken)
        {
            GenerateEmailConfirmationTokenCallCount++;
            var createdUser = CreatedUser;
            Assert.NotNull(createdUser);
            Assert.Equal(createdUser.Id.Value, userId);
            return Task.FromResult(_confirmationToken);
        }

        public Task<Identity?> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<bool> IsEmailConfirmationTokenValidAsync(Guid userId, string token, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task UpdateAsync(Identity user, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<AuthenticatedUser?> AuthenticateUserAsync(string login, string password, CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }

    private sealed class FakeTokenEncoder : ITokenEncoder
    {
        private readonly string _encodedToken;

        public FakeTokenEncoder(string encodedToken)
        {
            _encodedToken = encodedToken;
        }

        public int EncodeCallCount { get; private set; }

        public string EncodeForUrl(string token)
        {
            EncodeCallCount++;
            return _encodedToken;
        }

        public string DecodeFromUrl(string encodedToken) => throw new NotSupportedException();
    }

    private sealed class FakeConfirmationLinkBuilder : IConfirmationLinkBuilder
    {
        private readonly string _confirmationLink;

        public FakeConfirmationLinkBuilder(string confirmationLink)
        {
            _confirmationLink = confirmationLink;
        }

        public int BuildCallCount { get; private set; }

        public string BuildEmailConfirmationLink(Guid userId, string encodedToken)
        {
            BuildCallCount++;
            return _confirmationLink;
        }
    }

    private sealed class CapturingIntegrationEventPublisher : IIntegrationEventPublisher
    {
        public IntegrationEvent? PublishedEvent { get; private set; }

        public bool PublishedInsideTransaction { get; private set; }

        public bool InsideTransaction { get; set; }

        public Task PublishToOutboxAsync<TEvent>(TEvent message, CancellationToken cancellationToken)
            where TEvent : IntegrationEvent
        {
            PublishedEvent = message;
            PublishedInsideTransaction = InsideTransaction;
            return Task.CompletedTask;
        }
    }

    private sealed class TrackingUnitOfWork : IUnitOfWork
    {
        private readonly CapturingIntegrationEventPublisher _integrationEventPublisher;

        public TrackingUnitOfWork(CapturingIntegrationEventPublisher integrationEventPublisher)
        {
            _integrationEventPublisher = integrationEventPublisher;
        }

        public void Dispose()
        {
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
        {
            _integrationEventPublisher.InsideTransaction = true;

            try
            {
                return await operation(cancellationToken);
            }
            finally
            {
                _integrationEventPublisher.InsideTransaction = false;
            }
        }
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

