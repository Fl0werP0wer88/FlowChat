using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Application.Contracts.Infrastructure;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Application.Features.UserProfiles.Commands.SendEmailVerification;
using FlowChat.UserProfileService.Application.Features.UserProfiles.Queries.GetUserProfile;
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

        var result = await sut.Handle(new SendEmailVerificationCommand(profile.Id, email.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(emailVerificationRequestIssuer.LastRequestId, result.Value);
        Assert.Equal(email.Id, emailVerificationRequestIssuer.LastEmailId);
    }

    [Fact]
    public async Task Handle_WithConfirmedEmail_ReturnsValidationFailure()
    {
        var profile = CreateUserProfile("john@example.com", isConfirmed: true);
        var email = Assert.Single(profile.Emails);
        var repository = new TestUserProfileRepository(profile);
        var sut = new SendEmailVerificationCommandHandler(
            repository,
            new TestEmailVerificationRequestIssuer(),
            new TestUnitOfWork(),
            new TestDomainEventDispatcher());

        var result = await sut.Handle(new SendEmailVerificationCommand(profile.Id, email.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
        Assert.Equal($"Email '{email.Address}' is already confirmed.", result.Error.ErrorMessage);
    }

    private static UserProfileDto CreateUserProfile(string emailAddress, bool isConfirmed = false) =>
        new(
            Guid.NewGuid(),
            "jdoe",
            "John Doe",
            null,
            null,
            true,
            null,
            [
                new EmailDto(Guid.NewGuid(), EmailAddress.Create(emailAddress).Value, true, true, isConfirmed)
            ],
            []);

    private sealed class TestEmailVerificationRequestIssuer : IEmailVerificationRequestIssuer
    {
        public Guid? LastUserProfileId { get; private set; }
        public Guid? LastEmailId { get; private set; }
        public string? LastEmailAddress { get; private set; }
        public Guid LastRequestId { get; private set; }

        public Task<EmailVerificationRequest> IssueAsync(
            Guid userProfileId,
            Guid emailId,
            string emailAddress,
            CancellationToken cancellationToken)
        {
            LastUserProfileId = userProfileId;
            LastEmailId = emailId;
            LastEmailAddress = emailAddress;
            var verificationRequest = EmailVerificationRequest.Create(
                userProfileId,
                emailId,
                Guid.NewGuid().ToString("N"),
                DateTime.UtcNow.AddHours(24));
            LastRequestId = verificationRequest.Id.Value;

            return Task.FromResult(verificationRequest);
        }
    }

    private sealed class TestUserProfileRepository(UserProfileDto userProfile) : IUserProfileReadRepository
    {
        public Task<UserProfileDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(userProfile.Id == id ? userProfile : null);

        public Task<IReadOnlyList<UserProfileDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<UserProfileDto>>([]);

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
        public Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
