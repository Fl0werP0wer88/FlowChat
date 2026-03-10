using CSharpFunctionalExtensions;
using FlowChat.Application.Abstractions;
using FlowChat.Domain.Abstractions;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Application.UserProfiles.Commands.CreateInitialUserProfile;
using FlowChat.UserProfileService.Domain.Entities;
using FlowChat.UserProfileService.Domain.Events;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class CreateInitialUserProfileCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithEmailAndPhone_AddsContactsToAggregate()
    {
        var repository = new TestUserProfileRepository();
        var handler = new CreateInitialUserProfileCommandHandler(
            repository,
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
        Assert.Equal("john@example.com", email.Address);
        Assert.True(email.IsMain);
        Assert.Equal("+48123123123", phone.Number);
        Assert.True(phone.IsMain);
    }

    [Fact]
    public async Task Handle_WithNullContacts_ReturnsValidationFailure()
    {
        var repository = new TestUserProfileRepository();
        var handler = new CreateInitialUserProfileCommandHandler(
            repository,
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
        Assert.Null(repository.AddedEntity);
    }

    [Fact]
    public async Task Handle_WithWhitespaceContacts_ReturnsValidationFailure()
    {
        var repository = new TestUserProfileRepository();
        var handler = new CreateInitialUserProfileCommandHandler(
            repository,
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
        Assert.Null(repository.AddedEntity);
    }

    [Fact]
    public async Task Handle_WithSingleEmail_AddsMainEmailOnly()
    {
        var repository = new TestUserProfileRepository();
        var handler = new CreateInitialUserProfileCommandHandler(
            repository,
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
        Assert.True(email.IsMain);
        Assert.Empty(profile.Phones);
    }

    [Fact]
    public async Task Handle_WithSinglePhone_AddsMainPhoneOnly()
    {
        var repository = new TestUserProfileRepository();
        var handler = new CreateInitialUserProfileCommandHandler(
            repository,
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

        Assert.True(result.IsSuccess);
        var profile = Assert.IsType<UserProfile>(repository.AddedEntity);
        Assert.Empty(profile.Emails);
        var phone = Assert.Single(profile.Phones);
        Assert.True(phone.IsMain);
    }

    [Fact]
    public async Task Handle_AddsUserProfileCreatedDomainEventBeforeDispatchAndDispatchesIt()
    {
        var repository = new TestUserProfileRepository();
        var dispatcher = new TestDomainEventDispatcher();
        var handler = new CreateInitialUserProfileCommandHandler(
            repository,
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
        var addedEvent = Assert.IsType<UserProfileCreatedDomainEvent>(Assert.Single(repository.DomainEventsAtAdd));
        var dispatchedEvent = Assert.IsType<UserProfileCreatedDomainEvent>(Assert.Single(dispatcher.DispatchedEvents));
        Assert.Equal(addedEvent.UserProfileId, dispatchedEvent.UserProfileId);
        Assert.Equal("john@example.com", addedEvent.MainEmail);
        Assert.Null(addedEvent.MainPhone);
    }

    private sealed class TestUserProfileRepository : IUserProfileRepository
    {
        public UserProfile? AddedEntity { get; private set; }
        public IReadOnlyList<IDomainEvent> DomainEventsAtAdd { get; private set; } = [];

        public Task<UserProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<UserProfile?>(null);

        public Task<IReadOnlyList<UserProfile>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<UserProfile>>([]);

        public Task<UserProfile> AddAsync(UserProfile entity, CancellationToken cancellationToken = default)
        {
            AddedEntity = entity;
            DomainEventsAtAdd = entity.DomainEvents.ToList();
            return Task.FromResult(entity);
        }

        public Task UpdateAsync(UserProfile entity, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteAsync(UserProfile entity, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlyList<UserProfile>> GetActiveAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<UserProfile>>([]);

        public Task<UserProfile?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default) =>
            Task.FromResult<UserProfile?>(null);

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
