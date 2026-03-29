using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Application.Contracts.Infrastructure;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Application.Features.UserProfiles.EmailVerification;
using FlowChat.UserProfileService.Domain.Entities;
using FlowChat.UserProfileService.Infrastructure.Services;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class EmailVerificationRequestIssuerTests
{
    [Fact]
    public async Task IssueAsync_InvalidatesExistingRequestsAndPublishesIntegrationEvent()
    {
        var userProfile = CreateUserProfile("john@example.com");
        var email = Assert.Single(userProfile.Emails);
        var existingRequest = EmailVerificationRequest.Create(
            userProfile.Id,
            email.Id,
            "existing-nonce",
            DateTime.UtcNow.AddHours(6));
        var repository = new TestEmailVerificationRequestRepository([existingRequest]);
        var tokenProtector = new TestEmailVerificationTokenProtector();
        var linkBuilder = new TestEmailVerificationLinkBuilder();
        var integrationEventPublisher = new TestIntegrationEventPublisher();
        var sut = new EmailVerificationRequestIssuer(
            repository,
            tokenProtector,
            linkBuilder,
            integrationEventPublisher);

        var result = await sut.IssueAsync(userProfile, email, CancellationToken.None);

        Assert.NotNull(existingRequest.InvalidatedAtUtc);
        Assert.Same(result, repository.AddedEntity);
        Assert.NotNull(tokenProtector.LastPayload);
        Assert.Equal(userProfile.Id.Value, tokenProtector.LastPayload!.UserProfileId);
        Assert.Equal(email.Id.Value, tokenProtector.LastPayload.EmailId);
        Assert.Equal(result.Nonce, tokenProtector.LastPayload.Nonce);
        Assert.NotNull(integrationEventPublisher.LastPublishedEvent);
        Assert.Equal(result.Id.Value.ToString(), integrationEventPublisher.LastPublishedEvent!.Key);
        Assert.Equal(userProfile.Id.Value, integrationEventPublisher.LastPublishedEvent.UserId);
        Assert.Equal("john@example.com", integrationEventPublisher.LastPublishedEvent.UserEmail);
        Assert.Equal("https://frontend.flowchat.local/email-verification?token=protected-token", integrationEventPublisher.LastPublishedEvent.ConfirmationLink);
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

    private sealed class TestEmailVerificationRequestRepository(
        IReadOnlyList<EmailVerificationRequest>? activeRequests = null)
        : IEmailVerificationRequestWriteRepository
    {
        private readonly List<EmailVerificationRequest> _requests = activeRequests?.ToList() ?? [];

        public EmailVerificationRequest? AddedEntity { get; private set; }

        public Task<EmailVerificationRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_requests.FirstOrDefault(x => x.Id.Value == id));

        public Task<EmailVerificationRequest> AddAsync(EmailVerificationRequest entity, CancellationToken cancellationToken = default)
        {
            AddedEntity = entity;
            _requests.Add(entity);
            return Task.FromResult(entity);
        }

        public Task UpdateAsync(EmailVerificationRequest entity, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task DeleteAsync(EmailVerificationRequest entity, CancellationToken cancellationToken = default)
        {
            _requests.Remove(entity);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<EmailVerificationRequest>> GetActiveByEmailIdAsync(
            Guid emailId,
            CancellationToken cancellationToken = default)
        {
            var requests = _requests.Where(x => x.EmailId.Value == emailId && x.IsActive(DateTime.UtcNow)).ToArray();
            return Task.FromResult((IReadOnlyList<EmailVerificationRequest>)requests);
        }

        public Task<EmailVerificationRequest?> GetByNonceAsync(
            string nonce,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_requests.FirstOrDefault(x => x.Nonce == nonce));
    }

    private sealed class TestEmailVerificationTokenProtector : IEmailVerificationTokenProtector
    {
        public EmailVerificationTokenPayload? LastPayload { get; private set; }

        public string Protect(EmailVerificationTokenPayload payload)
        {
            LastPayload = payload;
            return "protected-token";
        }

        public bool TryUnprotect(string token, out EmailVerificationTokenPayload? payload)
        {
            payload = null;
            return false;
        }
    }

    private sealed class TestEmailVerificationLinkBuilder : IEmailVerificationLinkBuilder
    {
        public string BuildEmailVerificationLink(string token) =>
            $"https://frontend.flowchat.local/email-verification?token={token}";
    }

    private sealed class TestIntegrationEventPublisher : IIntegrationEventPublisher
    {
        public EmailVerificationRequestIntegrationEvent? LastPublishedEvent { get; private set; }

        public Task PublishToOutboxAsync<TEvent>(TEvent message, CancellationToken cancellationToken)
            where TEvent : IntegrationEvent
        {
            LastPublishedEvent = message as EmailVerificationRequestIntegrationEvent;
            return Task.CompletedTask;
        }
    }
}
