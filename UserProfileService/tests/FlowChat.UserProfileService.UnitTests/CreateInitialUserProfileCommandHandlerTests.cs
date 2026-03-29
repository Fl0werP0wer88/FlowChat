using CSharpFunctionalExtensions;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Application.Contracts.Infrastructure;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Application.Features.UserProfiles.Commands.CreateInitialUserProfile;
using FlowChat.UserProfileService.Application.Features.UserProfiles.Queries.GetUserProfile;
using FlowChat.UserProfileService.Domain.Entities;
using FlowChat.UserProfileService.Domain.Events;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class CreateInitialUserProfileCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithEmailAndPhone_AddsContactsToAggregate()
    {
        var repository = new TestUserProfileRepository();
        var emailVerificationRequestIssuer = new TestEmailVerificationRequestIssuer();
        var handler = new CreateInitialUserProfileCommandHandler(
            repository,
            repository,
            emailVerificationRequestIssuer,
            new TestUnitOfWork(),
            new TestDomainEventDispatcher());
        var userId = Guid.NewGuid();

        var result = await handler.Handle(
            new CreateInitialUserProfileCommand(
                "jdoe",
                "John Doe",
                null,
                null,
                "john@example.com",
                "+48123123123",
                userId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(userId, result.Value);
        var profile = Assert.IsType<UserProfile>(repository.AddedEntity);
        var email = Assert.Single(profile.Emails);
        var phone = Assert.Single(profile.Phones);
        Assert.Equal("john@example.com", email.Address.Value);
        Assert.True(email.IsMain);
        Assert.True(email.IsAuth);
        Assert.Equal("+48123123123", phone.Number.Value);
        Assert.True(phone.IsMain);
        Assert.Equal(email.Id.Value, emailVerificationRequestIssuer.LastEmailId);
    }

    [Fact]
    public async Task Handle_WithNullContacts_ReturnsValidationFailure()
    {
        var repository = new TestUserProfileRepository();
        var handler = new CreateInitialUserProfileCommandHandler(
            repository,
            repository,
            new TestEmailVerificationRequestIssuer(),
            new TestUnitOfWork(),
            new TestDomainEventDispatcher());

        var result = await handler.Handle(
            new CreateInitialUserProfileCommand(
                "jdoe",
                "John Doe",
                null,
                null,
                null,
                null,
                Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
        Assert.Equal(["Email is required."], result.Error.Errors);
        Assert.Null(repository.AddedEntity);
    }

    [Fact]
    public async Task Handle_WithWhitespaceContacts_ReturnsValidationFailure()
    {
        var repository = new TestUserProfileRepository();
        var handler = new CreateInitialUserProfileCommandHandler(
            repository,
            repository,
            new TestEmailVerificationRequestIssuer(),
            new TestUnitOfWork(),
            new TestDomainEventDispatcher());

        var result = await handler.Handle(
            new CreateInitialUserProfileCommand(
                "jdoe",
                "John Doe",
                null,
                null,
                "   ",
                "   ",
                Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
        Assert.Equal(["Email is required."], result.Error.Errors);
        Assert.Null(repository.AddedEntity);
    }

    [Fact]
    public async Task Handle_WithMissingRequiredFields_ReturnsValidationFailureWithAllErrorsInOrder()
    {
        var repository = new TestUserProfileRepository();
        var handler = new CreateInitialUserProfileCommandHandler(
            repository,
            repository,
            new TestEmailVerificationRequestIssuer(),
            new TestUnitOfWork(),
            new TestDomainEventDispatcher());

        var result = await handler.Handle(
            new CreateInitialUserProfileCommand(
                "   ",
                "   ",
                null,
                null,
                null,
                null,
                Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
        Assert.Equal("Validation Failed.", result.Error.ErrorMessage);
        Assert.Equal(
            [
                "UserName is required.",
                "DisplayName is required.",
                "Email is required."
            ],
            result.Error.Errors);
        Assert.Null(repository.AddedEntity);
    }

    [Fact]
    public async Task Handle_WithSingleEmail_AddsMainEmailOnly()
    {
        var repository = new TestUserProfileRepository();
        var emailVerificationRequestIssuer = new TestEmailVerificationRequestIssuer();
        var handler = new CreateInitialUserProfileCommandHandler(
            repository,
            repository,
            emailVerificationRequestIssuer,
            new TestUnitOfWork(),
            new TestDomainEventDispatcher());

        var result = await handler.Handle(
            new CreateInitialUserProfileCommand(
                "jdoe",
                "John Doe",
                null,
                null,
                "john@example.com",
                null,
                Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var profile = Assert.IsType<UserProfile>(repository.AddedEntity);
        var email = Assert.Single(profile.Emails);
        Assert.Equal("john@example.com", email.Address.Value);
        Assert.True(email.IsMain);
        Assert.True(email.IsAuth);
        Assert.Empty(profile.Phones);
        Assert.Equal(email.Id.Value, emailVerificationRequestIssuer.LastEmailId);
    }

    [Fact]
    public async Task Handle_WithInvalidEmail_ReturnsValidationFailure()
    {
        var repository = new TestUserProfileRepository();
        var handler = new CreateInitialUserProfileCommandHandler(
            repository,
            repository,
            new TestEmailVerificationRequestIssuer(),
            new TestUnitOfWork(),
            new TestDomainEventDispatcher());

        var result = await handler.Handle(
            new CreateInitialUserProfileCommand(
                "jdoe",
                "John Doe",
                null,
                null,
                "not-an-email",
                null,
                Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
        Assert.Equal([EmailAddress.InvalidEmailAddressMessage], result.Error.Errors);
        Assert.Null(repository.AddedEntity);
    }

    [Fact]
    public async Task Handle_WithSinglePhone_ReturnsValidationFailureBecauseEmailIsRequired()
    {
        var repository = new TestUserProfileRepository();
        var handler = new CreateInitialUserProfileCommandHandler(
            repository,
            repository,
            new TestEmailVerificationRequestIssuer(),
            new TestUnitOfWork(),
            new TestDomainEventDispatcher());

        var result = await handler.Handle(
            new CreateInitialUserProfileCommand(
                "jdoe",
                "John Doe",
                null,
                null,
                null,
                "+48123123123",
                Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
        Assert.Equal(["Email is required."], result.Error.Errors);
        Assert.Null(repository.AddedEntity);
    }

    [Fact]
    public async Task Handle_WithEmailAndFormattedPhone_NormalizesPhoneToE164()
    {
        var repository = new TestUserProfileRepository();
        var handler = new CreateInitialUserProfileCommandHandler(
            repository,
            repository,
            new TestEmailVerificationRequestIssuer(),
            new TestUnitOfWork(),
            new TestDomainEventDispatcher());

        var result = await handler.Handle(
            new CreateInitialUserProfileCommand(
                "jdoe",
                "John Doe",
                null,
                null,
                "john@example.com",
                "+48 123 123 123",
                Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var profile = Assert.IsType<UserProfile>(repository.AddedEntity);
        var phone = Assert.Single(profile.Phones);
        Assert.Equal("+48123123123", phone.Number.Value);
    }

    [Fact]
    public async Task Handle_WithEmailAndInvalidPhone_ReturnsValidationFailure()
    {
        var repository = new TestUserProfileRepository();
        var handler = new CreateInitialUserProfileCommandHandler(
            repository,
            repository,
            new TestEmailVerificationRequestIssuer(),
            new TestUnitOfWork(),
            new TestDomainEventDispatcher());

        var result = await handler.Handle(
            new CreateInitialUserProfileCommand(
                "jdoe",
                "John Doe",
                null,
                null,
                "john@example.com",
                "123123123",
                Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
        Assert.Equal([PhoneNumber.InvalidPhoneNumberMessage], result.Error.Errors);
        Assert.Null(repository.AddedEntity);
    }

    [Fact]
    public async Task Handle_AddsUserProfileCreatedDomainEventBeforeDispatchAndDispatchesIt()
    {
        var repository = new TestUserProfileRepository();
        var dispatcher = new TestDomainEventDispatcher();
        var emailVerificationRequestIssuer = new TestEmailVerificationRequestIssuer();
        var handler = new CreateInitialUserProfileCommandHandler(
            repository,
            repository,
            emailVerificationRequestIssuer,
            new TestUnitOfWork(),
            dispatcher);

        var result = await handler.Handle(
            new CreateInitialUserProfileCommand(
                "jdoe",
                "John Doe",
                null,
                null,
                "john@example.com",
                null,
                Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var addedEvent = Assert.Single(repository.DomainEventsAtAdd.OfType<UserProfileCreatedDomainEvent>());
        var dispatchedEvent = Assert.Single(dispatcher.DispatchedEvents.OfType<UserProfileCreatedDomainEvent>());
        Assert.Equal(addedEvent.UserProfileId, dispatchedEvent.UserProfileId);
        Assert.Equal("john@example.com", addedEvent.MainEmail);
        Assert.Null(addedEvent.MainPhone);
        var stateChangedEvent = Assert.Single(repository.DomainEventsAtAdd.OfType<AggregateStateChangedDomainEvent<UserProfile, UserProfileSnapshot>>());
        Assert.Equal("john@example.com", stateChangedEvent.AggregateState.MainEmail);
        Assert.Null(stateChangedEvent.AggregateState.MainPhone);
        Assert.Equal("john@example.com", emailVerificationRequestIssuer.LastEmailAddress);
    }

    private sealed class TestEmailVerificationRequestIssuer : IEmailVerificationRequestIssuer
    {
        public Guid? LastEmailId { get; private set; }
        public string? LastEmailAddress { get; private set; }

        public Task<EmailVerificationRequest> IssueAsync(
            UserProfile userProfile,
            Email email,
            CancellationToken cancellationToken)
        {
            LastEmailId = email.Id.Value;
            LastEmailAddress = email.Address.Value;

            return Task.FromResult(EmailVerificationRequest.Create(
                userProfile.Id,
                email.Id,
                Guid.NewGuid().ToString("N"),
                DateTime.UtcNow.AddHours(24)));
        }
    }

    private sealed class TestUserProfileRepository : IUserProfileReadRepository, IUserProfileWriteRepository
    {
        public UserProfile? AddedEntity { get; private set; }
        public IReadOnlyList<IDomainEvent> DomainEventsAtAdd { get; private set; } = [];

        public Task<UserProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<UserProfile?>(null);

        Task<UserProfileDto?> IReadRepository<UserProfileDto>.GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<UserProfileDto?>(null);

        Task<IReadOnlyList<UserProfileDto>> IReadRepository<UserProfileDto>.GetAllAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<UserProfileDto>>([]);

        public Task<UserProfile> AddAsync(UserProfile entity, CancellationToken cancellationToken = default)
        {
            AddedEntity = entity;
            DomainEventsAtAdd = entity.DomainEvents.ToList();
            return Task.FromResult(entity);
        }

        public Task UpdateAsync(UserProfile entity, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteAsync(UserProfile entity, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlyList<UserProfileDto>> GetActiveAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<UserProfileDto>>([]);

        public Task<UserProfileDto?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default) =>
            Task.FromResult<UserProfileDto?>(null);

        public Task<bool> EmailAddressExistsAsync(string emailAddress, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> UserNameExistsAsync(
            string userName,
            Guid? excludedUserId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }

    private sealed class TestUnitOfWork : IUnitOfWork
    {
        public void Dispose()
        {
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken) =>
            operation(cancellationToken);
    }

    private sealed class TestDomainEventDispatcher : IDomainEventDispatcher
    {
        public List<IDomainEvent> DispatchedEvents { get; } = [];

        public Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
        {
            DispatchedEvents.AddRange(domainEvents);
            return Task.CompletedTask;
        }
    }
}
