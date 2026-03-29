using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Application.Contracts.Infrastructure;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Application.Features.UserProfiles.Commands.SendEmailVerification;
using FlowChat.UserProfileService.Domain.Entities;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class SendEmailVerificationCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithUnconfirmedEmail_IssuesVerificationRequest()
    {
        var profile = CreateUserProfile("john@example.com");
        var email = Assert.Single(profile.Emails);
        var repository = new TestUserProfileRepository(profile);
        var emailVerificationRequestIssuer = new TestEmailVerificationRequestIssuer();
        var sut = new SendEmailVerificationCommandHandler(
            repository,
            emailVerificationRequestIssuer,
            new TestUnitOfWork(),
            new TestDomainEventDispatcher());

        var result = await sut.Handle(new SendEmailVerificationCommand(profile.Id.Value, email.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(emailVerificationRequestIssuer.LastRequestId, result.Value);
        Assert.Equal(email.Id.Value, emailVerificationRequestIssuer.LastEmailId);
    }

    [Fact]
    public async Task Handle_WithConfirmedEmail_ReturnsValidationFailure()
    {
        var profile = CreateUserProfile("john@example.com");
        var email = Assert.Single(profile.Emails);
        profile.ConfirmEmail(email.Id);
        profile.ClearEvents();
        var repository = new TestUserProfileRepository(profile);
        var sut = new SendEmailVerificationCommandHandler(
            repository,
            new TestEmailVerificationRequestIssuer(),
            new TestUnitOfWork(),
            new TestDomainEventDispatcher());

        var result = await sut.Handle(new SendEmailVerificationCommand(profile.Id.Value, email.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
        Assert.Equal($"Email '{email.Address.Value}' is already confirmed.", result.Error.ErrorMessage);
    }

    private static UserProfile CreateUserProfile(string emailAddress)
    {
        var userProfileId = Id<UserProfile>.New();

        return UserProfile.Rehydrate(
            "jdoe",
            "John Doe",
            emails:
            [
                Email.Create(
                    userProfileId,
                    EmailAddress.Create(emailAddress),
                    isMain: true,
                    isAuth: true)
            ],
            id: userProfileId);
    }

    private sealed class TestEmailVerificationRequestIssuer : IEmailVerificationRequestIssuer
    {
        public Guid? LastEmailId { get; private set; }
        public Guid LastRequestId { get; private set; }

        public Task<EmailVerificationRequest> IssueAsync(
            UserProfile userProfile,
            Email email,
            CancellationToken cancellationToken)
        {
            LastEmailId = email.Id.Value;
            var verificationRequest = EmailVerificationRequest.Create(
                userProfile.Id,
                email.Id,
                Guid.NewGuid().ToString("N"),
                DateTime.UtcNow.AddHours(24));
            LastRequestId = verificationRequest.Id.Value;

            return Task.FromResult(verificationRequest);
        }
    }

    private sealed class TestUserProfileRepository(UserProfile userProfile) : IUserProfileWriteRepository
    {
        public Task<UserProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(userProfile.Id.Value == id ? userProfile : null);

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
