using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Application.Features.UserProfiles.Commands.AddPhone;
using FlowChat.UserProfileService.Domain.Entities;
using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class AddPhoneCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithEmptyUserIdAndMissingNumber_ReturnsSingleValidationFailureWithBothErrors()
    {
        var handler = new AddPhoneCommandHandler(
            new TestUserProfileRepository(),
            new TestUnitOfWork(),
            new TestDomainEventDispatcher());

        var result = await handler.Handle(new AddPhoneCommand(Guid.Empty, null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
        Assert.Equal("Validation Failed.", result.Error.ErrorMessage);
        Assert.Equal(["UserId is required.", "Phone number is required."], result.Error.Errors);
    }

    [Fact]
    public async Task Handle_WithInvalidNumber_ReturnsValidationFailure()
    {
        var profile = UserProfile.Rehydrate("jdoe", "John Doe", id: Id<UserProfile>.New());
        var handler = new AddPhoneCommandHandler(
            new TestUserProfileRepository(profile),
            new TestUnitOfWork(),
            new TestDomainEventDispatcher());

        var result = await handler.Handle(new AddPhoneCommand(profile.Id.Value, "123123123"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
        Assert.Equal([PhoneNumber.InvalidPhoneNumberMessage], result.Error.Errors);
    }

    [Fact]
    public async Task Handle_WithFormattedDuplicateNumber_ReturnsConflict()
    {
        var profile = UserProfile.Rehydrate("jdoe", "John Doe", id: Id<UserProfile>.New());
        profile.AddPhone("+48123123123");
        profile.ClearEvents();
        var handler = new AddPhoneCommandHandler(
            new TestUserProfileRepository(profile),
            new TestUnitOfWork(),
            new TestDomainEventDispatcher());

        var result = await handler.Handle(new AddPhoneCommand(profile.Id.Value, "+48 123 123 123"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.ErrorType);
        Assert.Equal("Phone '+48123123123' already exists.", result.Error.ErrorMessage);
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

