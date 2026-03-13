using FlowChat.Application.Abstractions;
using FlowChat.Domain.Abstractions;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Application.UserProfiles.Commands.SetMainEmail;
using FlowChat.UserProfileService.Application.UserProfiles.Commands.SetMainPhone;
using FlowChat.UserProfileService.Domain.Entities;
using FlowChat.UserProfileService.Domain.Events;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class SetMainContactCommandHandlerTests
{
    [Fact]
    public async Task SetMainEmail_WhenEmailExists_SetsMainEmailAndDispatchesDomainEvents()
    {
        var profile = UserProfile.Rehydrate("jdoe", "John Doe", id: Id<UserProfile>.New());
        var firstEmail = profile.AddEmail("john@example.com");
        var secondEmail = profile.AddEmail("john.secondary@example.com");
        profile.ClearEvents();

        var repository = new TestUserProfileRepository(profile);
        var dispatcher = new TestDomainEventDispatcher();
        var handler = new SetMainEmailCommandHandler(repository, new TestUnitOfWork(), dispatcher);

        var result = await handler.Handle(new SetMainEmailCommand(profile.Id.Value, secondEmail.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(secondEmail.Id.Value, result.Value);
        Assert.False(firstEmail.IsMain);
        Assert.True(secondEmail.IsMain);
        var emailChangedEvent = Assert.Single(dispatcher.DispatchedEvents.OfType<MainEmailChangedDomainEvent>());
        Assert.Equal(secondEmail.Id.Value, emailChangedEvent.EmailId);
        var stateChangedEvent = Assert.Single(dispatcher.DispatchedEvents.OfType<AggregateStateChangedDomainEvent<UserProfile, UserProfileSnapshot>>());
        Assert.Equal(secondEmail.Address, stateChangedEvent.AggregateState.MainEmail);
    }

    [Fact]
    public async Task SetMainEmail_WhenEmailDoesNotExist_ReturnsNotFound()
    {
        var profile = UserProfile.Rehydrate("jdoe", "John Doe", id: Id<UserProfile>.New());
        profile.AddEmail("john@example.com");
        profile.ClearEvents();

        var repository = new TestUserProfileRepository(profile);
        var handler = new SetMainEmailCommandHandler(repository, new TestUnitOfWork(), new TestDomainEventDispatcher());

        var result = await handler.Handle(new SetMainEmailCommand(profile.Id.Value, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.ErrorType);
    }

    [Fact]
    public async Task SetMainEmail_WhenEmailIdIsEmpty_ReturnsValidationFailure()
    {
        var handler = new SetMainEmailCommandHandler(
            new TestUserProfileRepository(),
            new TestUnitOfWork(),
            new TestDomainEventDispatcher());

        var result = await handler.Handle(new SetMainEmailCommand(Guid.NewGuid(), Guid.Empty), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
        Assert.Equal(["EmailId is required."], result.Error.Errors);
    }

    [Fact]
    public async Task SetMainEmail_WhenUserIdAndEmailIdAreEmpty_ReturnsValidationFailureWithBothErrors()
    {
        var handler = new SetMainEmailCommandHandler(
            new TestUserProfileRepository(),
            new TestUnitOfWork(),
            new TestDomainEventDispatcher());

        var result = await handler.Handle(new SetMainEmailCommand(Guid.Empty, Guid.Empty), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
        Assert.Equal("Validation Failed.", result.Error.ErrorMessage);
        Assert.Equal(["UserId is required.", "EmailId is required."], result.Error.Errors);
    }

    [Fact]
    public async Task SetMainPhone_WhenPhoneExists_SetsMainPhoneAndDispatchesDomainEvents()
    {
        var profile = UserProfile.Rehydrate("jdoe", "John Doe", id: Id<UserProfile>.New());
        var firstPhone = profile.AddPhone("+48123123123");
        var secondPhone = profile.AddPhone("+48987654321");
        profile.ClearEvents();

        var repository = new TestUserProfileRepository(profile);
        var dispatcher = new TestDomainEventDispatcher();
        var handler = new SetMainPhoneCommandHandler(repository, new TestUnitOfWork(), dispatcher);

        var result = await handler.Handle(new SetMainPhoneCommand(profile.Id.Value, secondPhone.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(secondPhone.Id.Value, result.Value);
        Assert.False(firstPhone.IsMain);
        Assert.True(secondPhone.IsMain);
        var phoneChangedEvent = Assert.Single(dispatcher.DispatchedEvents.OfType<MainPhoneChangedDomainEvent>());
        Assert.Equal(secondPhone.Id.Value, phoneChangedEvent.PhoneId);
        var stateChangedEvent = Assert.Single(dispatcher.DispatchedEvents.OfType<AggregateStateChangedDomainEvent<UserProfile, UserProfileSnapshot>>());
        Assert.Equal(secondPhone.Number, stateChangedEvent.AggregateState.MainPhone);
    }

    [Fact]
    public async Task SetMainPhone_WhenPhoneDoesNotExist_ReturnsNotFound()
    {
        var profile = UserProfile.Rehydrate("jdoe", "John Doe", id: Id<UserProfile>.New());
        profile.AddPhone("+48123123123");
        profile.ClearEvents();

        var repository = new TestUserProfileRepository(profile);
        var handler = new SetMainPhoneCommandHandler(repository, new TestUnitOfWork(), new TestDomainEventDispatcher());

        var result = await handler.Handle(new SetMainPhoneCommand(profile.Id.Value, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.ErrorType);
    }

    [Fact]
    public async Task SetMainPhone_WhenPhoneIdIsEmpty_ReturnsValidationFailure()
    {
        var handler = new SetMainPhoneCommandHandler(
            new TestUserProfileRepository(),
            new TestUnitOfWork(),
            new TestDomainEventDispatcher());

        var result = await handler.Handle(new SetMainPhoneCommand(Guid.NewGuid(), Guid.Empty), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
        Assert.Equal(["PhoneId is required."], result.Error.Errors);
    }

    [Fact]
    public async Task SetMainPhone_WhenUserIdAndPhoneIdAreEmpty_ReturnsValidationFailureWithBothErrors()
    {
        var handler = new SetMainPhoneCommandHandler(
            new TestUserProfileRepository(),
            new TestUnitOfWork(),
            new TestDomainEventDispatcher());

        var result = await handler.Handle(new SetMainPhoneCommand(Guid.Empty, Guid.Empty), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
        Assert.Equal("Validation Failed.", result.Error.ErrorMessage);
        Assert.Equal(["UserId is required.", "PhoneId is required."], result.Error.Errors);
    }

    private sealed class TestUserProfileRepository(UserProfile? userProfile = null) : IUserProfileRepository
    {
        public Task<UserProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<UserProfile?>(userProfile);

        public Task<IReadOnlyList<UserProfile>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<UserProfile>>([]);

        public Task<UserProfile> AddAsync(UserProfile entity, CancellationToken cancellationToken = default) =>
            Task.FromResult(entity);

        public Task UpdateAsync(UserProfile entity, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task DeleteAsync(UserProfile entity, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<UserProfile>> GetActiveAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<UserProfile>>([]);

        public Task<UserProfile?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default) =>
            Task.FromResult<UserProfile?>(null);

        public Task<UserProfile?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(userProfile?.Id.Value == id ? userProfile : null);

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
