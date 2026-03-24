using FlowChat.Application.Abstractions;
using FlowChat.Domain.Abstractions;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Application.Features.UserProfiles.Commands.AddEmail;
using FlowChat.UserProfileService.Domain.Entities;
using FlowChat.Domain.Abstractions.ValueObjects;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class AddEmailCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithEmptyUserIdAndMissingAddress_ReturnsSingleValidationFailureWithBothErrors()
    {
        var handler = new AddEmailCommandHandler(
            new TestUserProfileRepository(),
            new TestUnitOfWork(),
            new TestDomainEventDispatcher());

        var result = await handler.Handle(new AddEmailCommand(Guid.Empty, null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
        Assert.Equal("Validation Failed.", result.Error.ErrorMessage);
        Assert.Equal(["UserId is required.", "Email address is required."], result.Error.Errors);
    }

    [Fact]
    public async Task Handle_WithInvalidAddress_ReturnsValidationFailure()
    {
        var profile = UserProfile.Rehydrate("jdoe", "John Doe", id: Id<UserProfile>.New());
        var handler = new AddEmailCommandHandler(
            new TestUserProfileRepository(profile),
            new TestUnitOfWork(),
            new TestDomainEventDispatcher());

        var result = await handler.Handle(new AddEmailCommand(profile.Id.Value, "not-an-email"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
        Assert.Equal([EmailAddress.InvalidEmailAddressMessage], result.Error.Errors);
    }

    [Fact]
    public async Task Handle_WithDuplicateAddressIgnoringCase_ReturnsConflict()
    {
        var profile = UserProfile.Rehydrate("jdoe", "John Doe", id: Id<UserProfile>.New());
        profile.AddEmail("john@example.com");
        profile.ClearEvents();
        var handler = new AddEmailCommandHandler(
            new TestUserProfileRepository(profile),
            new TestUnitOfWork(),
            new TestDomainEventDispatcher());

        var result = await handler.Handle(new AddEmailCommand(profile.Id.Value, "JOHN@example.com"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.ErrorType);
        Assert.Equal("Email 'john@example.com' already exists.", result.Error.ErrorMessage);
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
        public Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
