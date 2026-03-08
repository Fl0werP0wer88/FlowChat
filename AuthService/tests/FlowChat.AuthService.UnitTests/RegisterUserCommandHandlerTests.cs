using FlowChat.Application.Abstractions;
using FlowChat.AuthService.Application.Commands;
using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.AuthService.Application.Handlers;
using FlowChat.AuthService.Application.Models;
using FlowChat.AuthService.Domain.Entities;
using FlowChat.AuthService.Domain.Events;
using FlowChat.Domain.Abstractions;
using FlowChat.Messaging.Contracts;
using FlowChat.Messaging.Contracts.AuthService.Events;

namespace FlowChat.AuthService.UnitTests;

public sealed class RegisterUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_PublishesEmailVerificationIntegrationEvent_InsideTransaction()
    {
        var repository = new FakeIdentityRepository("raw-confirmation-token");
        var tokenEncoder = new FakeTokenEncoder("encoded-confirmation-token");
        var confirmationLinkBuilder = new FakeConfirmationLinkBuilder("https://localhost/confirm?token=encoded-confirmation-token");
        var integrationEventPublisher = new CapturingIntegrationEventPublisher();
        var unitOfWork = new TrackingUnitOfWork(integrationEventPublisher);
        var domainEventDispatcher = new CapturingDomainEventDispatcher();
        var handler = new RegisterUserCommandHandler(
            repository,
            tokenEncoder,
            confirmationLinkBuilder,
            integrationEventPublisher,
            unitOfWork,
            domainEventDispatcher);

        var result = await handler.Handle(
            new RegisterUserCommand
            {
                UserName = "flower",
                Email = "flower@example.com",
                PhoneNumber = "+48123123123",
                Password = "P@ssw0rd!"
            },
            CancellationToken.None);
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

        var dispatchedEvents = Assert.Single(domainEventDispatcher.DispatchedBatches);
        var userCreatedDomainEvent = Assert.IsType<UserCreatedDomainEvent>(Assert.Single(dispatchedEvents));
        Assert.Equal(createdUserId, userCreatedDomainEvent.UserId.Value);
    }

    private sealed class FakeIdentityRepository : IIdentityRepository
    {
        private readonly string _confirmationToken;

        public FakeIdentityRepository(string confirmationToken)
        {
            _confirmationToken = confirmationToken;
        }

        public Identity? CreatedUser { get; private set; }

        public Task<Guid> CreateUserAsync(Identity user, string password, CancellationToken cancellationToken)
        {
            CreatedUser = user;
            return Task.FromResult(user.Id.Value);
        }

        public Task<string> GenerateEmailConfirmationTokenAsync(Guid userId, CancellationToken cancellationToken)
        {
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

        public string EncodeForUrl(string token) => _encodedToken;

        public string DecodeFromUrl(string encodedToken) => throw new NotSupportedException();
    }

    private sealed class FakeConfirmationLinkBuilder : IConfirmationLinkBuilder
    {
        private readonly string _confirmationLink;

        public FakeConfirmationLinkBuilder(string confirmationLink)
        {
            _confirmationLink = confirmationLink;
        }

        public string BuildEmailConfirmationLink(Guid userId, string encodedToken) => _confirmationLink;
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
