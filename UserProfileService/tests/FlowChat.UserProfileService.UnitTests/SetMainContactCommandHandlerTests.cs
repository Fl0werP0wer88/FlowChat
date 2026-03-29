using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Application.Features.UserProfiles.Commands.SetMainEmail;
using FlowChat.UserProfileService.Application.Features.UserProfiles.Commands.SetMainPhone;
using FlowChat.UserProfileService.Domain.Entities;
using FlowChat.UserProfileService.Domain.Events;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class SetMainContactCommandHandlerTests
{
    [Fact]
    public async Task SetMainEmail_WhenEmailExists_SetsMainEmailAndDispatchesDomainEvents()
    {
        var profile = CreateUserProfile();
        var firstEmail = Assert.Single(profile.Emails);
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
        Assert.Equal(profile.Id, emailChangedEvent.UserProfileId);
        Assert.Equal(secondEmail.Id, emailChangedEvent.EmailId);
        Assert.Equal(secondEmail.Address, emailChangedEvent.Address);
        var stateChangedEvent = Assert.Single(dispatcher.DispatchedEvents.OfType<AggregateStateChangedDomainEvent<UserProfile, UserProfileSnapshot>>());
        Assert.Equal(secondEmail.Address.Value, stateChangedEvent.AggregateState.MainEmail);
    }

    [Fact]
    public async Task SetMainEmail_WhenEmailDoesNotExist_ReturnsNotFound()
    {
        var profile = CreateUserProfile();
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
        var profile = CreateUserProfile();
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
        Assert.Equal(profile.Id, phoneChangedEvent.UserProfileId);
        Assert.Equal(secondPhone.Id, phoneChangedEvent.PhoneId);
        Assert.Equal(secondPhone.Number, phoneChangedEvent.Number);
        var stateChangedEvent = Assert.Single(dispatcher.DispatchedEvents.OfType<AggregateStateChangedDomainEvent<UserProfile, UserProfileSnapshot>>());
        Assert.Equal(secondPhone.Number.Value, stateChangedEvent.AggregateState.MainPhone);
    }

    [Fact]
    public async Task SetMainPhone_WhenPhoneDoesNotExist_ReturnsNotFound()
    {
        var profile = CreateUserProfile();
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

    private static UserProfile CreateUserProfile()
    {
        var profile = UserProfile.Create("jdoe", "John Doe", EmailAddress.Create("john@example.com"), id: Id<UserProfile>.New());
        profile.ClearEvents();
        return profile;
    }

    private sealed class TestUserProfileRepository(UserProfile? userProfile = null) : IUserProfileWriteRepository
    {
        public Task<UserProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(userProfile?.Id.Value == id ? userProfile : null);

        public Task<UserProfile> AddAsync(UserProfile entity, CancellationToken cancellationToken = default) =>
            Task.FromResult(entity);

        public Task UpdateAsync(UserProfile entity, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task DeleteAsync(UserProfile entity, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
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
