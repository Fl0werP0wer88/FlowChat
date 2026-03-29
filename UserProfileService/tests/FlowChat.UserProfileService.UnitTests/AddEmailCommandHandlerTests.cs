using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Application.Contracts.Infrastructure;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Application.Features.UserProfiles.Commands.AddEmail;
using FlowChat.UserProfileService.Application.Features.UserProfiles.Queries.GetUserProfile;
using FlowChat.UserProfileService.Domain.Entities;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class AddEmailCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithEmptyUserIdAndMissingAddress_ReturnsSingleValidationFailureWithBothErrors()
    {
        var repository = new TestUserProfileRepository();
        var handler = new AddEmailCommandHandler(
            repository,
            repository,
            new TestEmailVerificationRequestIssuer(),
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
        var profile = CreateUserProfile("primary@example.com");
        var repository = new TestUserProfileRepository(profile);
        var handler = new AddEmailCommandHandler(
            repository,
            repository,
            new TestEmailVerificationRequestIssuer(),
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
        var profile = CreateUserProfile("john@example.com");
        profile.ClearEvents();
        var repository = new TestUserProfileRepository(profile);
        var handler = new AddEmailCommandHandler(
            repository,
            repository,
            new TestEmailVerificationRequestIssuer(),
            new TestUnitOfWork(),
            new TestDomainEventDispatcher());

        var result = await handler.Handle(new AddEmailCommand(profile.Id.Value, "JOHN@example.com"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.ErrorType);
        Assert.Equal("Email 'john@example.com' is already taken.", result.Error.ErrorMessage);
    }

    [Fact]
    public async Task Handle_WithUniqueAddress_AddsEmailAndIssuesVerificationRequest()
    {
        var profile = CreateUserProfile("primary@example.com");
        profile.ClearEvents();
        var repository = new TestUserProfileRepository(profile);
        var emailVerificationRequestIssuer = new TestEmailVerificationRequestIssuer();
        var handler = new AddEmailCommandHandler(
            repository,
            repository,
            emailVerificationRequestIssuer,
            new TestUnitOfWork(),
            new TestDomainEventDispatcher());

        var result = await handler.Handle(new AddEmailCommand(profile.Id.Value, "secondary@example.com"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var addedEmail = Assert.Single(profile.Emails.Where(x => x.Address.Value == "secondary@example.com"));
        Assert.Equal(addedEmail.Id.Value, result.Value);
        Assert.Equal(addedEmail.Id.Value, emailVerificationRequestIssuer.LastEmailId);
        Assert.Equal("secondary@example.com", emailVerificationRequestIssuer.LastEmailAddress);
    }

    private static UserProfile CreateUserProfile(string emailAddress)
    {
        var userProfile = UserProfile.Create("jdoe", "John Doe", EmailAddress.Create(emailAddress), id: Id<UserProfile>.New());
        userProfile.ClearEvents();
        return userProfile;
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

    private sealed class TestUserProfileRepository(UserProfile? userProfile = null)
        : IUserProfileReadRepository, IUserProfileWriteRepository
    {
        public Task<UserProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(userProfile?.Id.Value == id ? userProfile : null);

        Task<UserProfileDto?> IReadRepository<UserProfileDto>.GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<UserProfileDto?>(null);

        Task<IReadOnlyList<UserProfileDto>> IReadRepository<UserProfileDto>.GetAllAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<UserProfileDto>>([]);

        public Task<UserProfile> AddAsync(UserProfile entity, CancellationToken cancellationToken = default) =>
            Task.FromResult(entity);

        public Task UpdateAsync(UserProfile entity, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task DeleteAsync(UserProfile entity, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<UserProfileDto>> GetActiveAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<UserProfileDto>>([]);

        public Task<UserProfileDto?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default) =>
            Task.FromResult<UserProfileDto?>(null);

        public Task<bool> EmailAddressExistsAsync(string emailAddress, CancellationToken cancellationToken = default) =>
            Task.FromResult(userProfile?.Emails.Any(x => x.Address == EmailAddress.Create(emailAddress)) ?? false);

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
        public Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
