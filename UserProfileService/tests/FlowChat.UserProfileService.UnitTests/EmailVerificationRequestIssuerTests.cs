using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.Shared.Application;
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
        var userProfileId = Guid.NewGuid();
        var emailId = Guid.NewGuid();
        var existingRequest = EmailVerificationRequest.Create(
            userProfileId,
            emailId,
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

        var result = await sut.IssueAsync(userProfileId, emailId, "john@example.com", CancellationToken.None);

        Assert.NotNull(existingRequest.InvalidatedAtUtc);
        Assert.Same(result, repository.AddedEntity);
        Assert.NotNull(tokenProtector.LastPayload);
        Assert.Equal(userProfileId, tokenProtector.LastPayload!.UserProfileId);
        Assert.Equal(emailId, tokenProtector.LastPayload.EmailId);
        Assert.Equal(result.Nonce, tokenProtector.LastPayload.Nonce);
        Assert.NotNull(integrationEventPublisher.LastPublishedEvent);
        Assert.Equal(result.Id.Value.ToString(), integrationEventPublisher.LastPublishedEvent!.Key);
        Assert.Equal(userProfileId, integrationEventPublisher.LastPublishedEvent.UserId);
        Assert.Equal("john@example.com", integrationEventPublisher.LastPublishedEvent.UserEmail);
        Assert.Equal("https://frontend.flowchat.local/email-verification?token=protected-token", integrationEventPublisher.LastPublishedEvent.ConfirmationLink);
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
