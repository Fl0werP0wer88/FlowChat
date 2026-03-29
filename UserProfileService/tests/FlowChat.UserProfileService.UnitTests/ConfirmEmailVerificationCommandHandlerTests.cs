using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Application.Contracts.Infrastructure;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Application.Features.UserProfiles.Commands.ConfirmEmailVerification;
using FlowChat.UserProfileService.Application.Features.UserProfiles.EmailVerification;
using FlowChat.UserProfileService.Domain.Entities;
using MediatR;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class ConfirmEmailVerificationCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidToken_ConfirmsEmailAndConsumesRequest()
    {
        var profile = CreateUserProfile("john@example.com");
        var email = Assert.Single(profile.Emails);
        var verificationRequest = EmailVerificationRequest.Create(
            profile.Id,
            email.Id,
            "valid-nonce",
            DateTime.UtcNow.AddHours(24));
        var userProfileRepository = new TestUserProfileRepository(profile);
        var verificationRequestRepository = new TestEmailVerificationRequestRepository(verificationRequest);
        var sut = new ConfirmEmailVerificationCommandHandler(
            userProfileRepository,
            verificationRequestRepository,
            new TestEmailVerificationTokenProtector(new EmailVerificationTokenPayload(profile.Id.Value, email.Id.Value, verificationRequest.Nonce)),
            new TestUnitOfWork(),
            new TestDomainEventDispatcher());

        var result = await sut.Handle(new ConfirmEmailVerificationCommand("valid-token"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(Unit.Value, result.Value);
        Assert.True(email.IsConfirmed);
        Assert.NotNull(verificationRequest.ConsumedAtUtc);
    }

    [Fact]
    public async Task Handle_WithTamperedToken_ReturnsValidationFailure()
    {
        var sut = new ConfirmEmailVerificationCommandHandler(
            new TestUserProfileRepository(null),
            new TestEmailVerificationRequestRepository(null),
            new TestEmailVerificationTokenProtector(null),
            new TestUnitOfWork(),
            new TestDomainEventDispatcher());

        var result = await sut.Handle(new ConfirmEmailVerificationCommand("invalid-token"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
        Assert.Equal("Email verification link is invalid or has expired.", result.Error.ErrorMessage);
    }

    [Fact]
    public async Task Handle_WithConsumedRequest_ReturnsValidationFailure()
    {
        var profile = CreateUserProfile("john@example.com");
        var email = Assert.Single(profile.Emails);
        var verificationRequest = EmailVerificationRequest.Create(
            profile.Id,
            email.Id,
            "used-nonce",
            DateTime.UtcNow.AddHours(24));
        verificationRequest.Consume(DateTime.UtcNow);
        var sut = new ConfirmEmailVerificationCommandHandler(
            new TestUserProfileRepository(profile),
            new TestEmailVerificationRequestRepository(verificationRequest),
            new TestEmailVerificationTokenProtector(new EmailVerificationTokenPayload(profile.Id.Value, email.Id.Value, verificationRequest.Nonce)),
            new TestUnitOfWork(),
            new TestDomainEventDispatcher());

        var result = await sut.Handle(new ConfirmEmailVerificationCommand("used-token"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
        Assert.Equal("Email verification link is invalid or has expired.", result.Error.ErrorMessage);
    }

    [Fact]
    public async Task Handle_WithMissingProfile_ReturnsNotFound()
    {
        var userProfileId = Guid.NewGuid();
        var emailId = Guid.NewGuid();
        var verificationRequest = EmailVerificationRequest.Create(
            Id<UserProfile>.FromGuid(userProfileId),
            Id<Email>.FromGuid(emailId),
            "missing-profile-nonce",
            DateTime.UtcNow.AddHours(24));
        var sut = new ConfirmEmailVerificationCommandHandler(
            new TestUserProfileRepository(null),
            new TestEmailVerificationRequestRepository(verificationRequest),
            new TestEmailVerificationTokenProtector(new EmailVerificationTokenPayload(userProfileId, emailId, verificationRequest.Nonce)),
            new TestUnitOfWork(),
            new TestDomainEventDispatcher());

        var result = await sut.Handle(new ConfirmEmailVerificationCommand("valid-token"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.ErrorType);
        Assert.Equal($"User profile '{userProfileId}' was not found.", result.Error.ErrorMessage);
    }

    private static UserProfile CreateUserProfile(string emailAddress)
    {
        var userProfile = UserProfile.Create("jdoe", "John Doe", EmailAddress.Create(emailAddress), id: Id<UserProfile>.New());
        userProfile.ClearEvents();
        return userProfile;
    }

    private sealed class TestEmailVerificationTokenProtector(EmailVerificationTokenPayload? payload)
        : IEmailVerificationTokenProtector
    {
        public string Protect(EmailVerificationTokenPayload tokenPayload) => "unused";

        public bool TryUnprotect(string token, out EmailVerificationTokenPayload? unprotectedPayload)
        {
            unprotectedPayload = payload;
            return payload is not null;
        }
    }

    private sealed class TestUserProfileRepository(UserProfile? userProfile) : IUserProfileWriteRepository
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

    private sealed class TestEmailVerificationRequestRepository(EmailVerificationRequest? verificationRequest)
        : IEmailVerificationRequestWriteRepository
    {
        public Task<EmailVerificationRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(verificationRequest?.Id.Value == id ? verificationRequest : null);

        public Task<EmailVerificationRequest> AddAsync(EmailVerificationRequest entity, CancellationToken cancellationToken = default) =>
            Task.FromResult(entity);

        public Task UpdateAsync(EmailVerificationRequest entity, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task DeleteAsync(EmailVerificationRequest entity, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<EmailVerificationRequest>> GetActiveByEmailIdAsync(
            Guid emailId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<EmailVerificationRequest>>([]);

        public Task<EmailVerificationRequest?> GetByNonceAsync(
            string nonce,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(verificationRequest?.Nonce == nonce ? verificationRequest : null);
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
