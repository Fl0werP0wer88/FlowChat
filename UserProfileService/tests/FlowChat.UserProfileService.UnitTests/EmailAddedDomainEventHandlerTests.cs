using FlowChat.UserProfileService.Application.Common.Eventing.Handlers;
using FlowChat.UserProfileService.Application.Contracts.Infrastructure;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Domain.Entities;
using FlowChat.UserProfileService.Domain.Events;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class EmailAddedDomainEventHandlerTests
{
    [Fact]
    public async Task Handle_IssuesVerificationRequestForAddedEmail()
    {
        var issuer = new TestEmailVerificationRequestIssuer();
        var handler = new EmailAddedDomainEventHandler(issuer);
        var userProfileId = Guid.NewGuid();
        var emailId = Guid.NewGuid();
        var domainEvent = new EmailAddedDomainEvent(userProfileId, emailId, EmailAddress.Create("secondary@example.com"));

        await handler.Handle(domainEvent, CancellationToken.None);

        Assert.Equal(userProfileId, issuer.LastUserProfileId);
        Assert.Equal(emailId, issuer.LastEmailId);
        Assert.Equal("secondary@example.com", issuer.LastEmailAddress);
    }

    private sealed class TestEmailVerificationRequestIssuer : IEmailVerificationRequestIssuer
    {
        public Guid? LastUserProfileId { get; private set; }
        public Guid? LastEmailId { get; private set; }
        public string? LastEmailAddress { get; private set; }

        public Task<EmailVerificationRequest> IssueAsync(
            Guid userProfileId,
            Guid emailId,
            string emailAddress,
            CancellationToken cancellationToken)
        {
            LastUserProfileId = userProfileId;
            LastEmailId = emailId;
            LastEmailAddress = emailAddress;

            return Task.FromResult(EmailVerificationRequest.Create(
                userProfileId,
                emailId,
                Guid.NewGuid().ToString("N"),
                DateTime.UtcNow.AddHours(24)));
        }
    }
}
