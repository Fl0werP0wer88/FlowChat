using FlowChat.UserProfileService.Application.Common.Eventing.Handlers;
using FlowChat.UserProfileService.Application.Contracts.Infrastructure;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;
using FlowChat.UserProfileService.Domain.Events;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class EmailAddedDomainEventHandlerTests
{
    private readonly Mock<IEmailVerificationRequestIssuer> _issuerMock = new();

    public EmailAddedDomainEventHandlerTests()
    {
        _issuerMock
            .Setup(x => x.IssueAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid userProfileId, Guid emailId, string _, CancellationToken _) =>
                EmailVerificationRequest.Create(userProfileId, emailId, Guid.NewGuid().ToString("N"), DateTime.UtcNow.AddHours(24)));
    }

    [Fact]
    public async Task Handle_IssuesVerificationRequestForAddedEmail()
    {
        var handler = new EmailAddedDomainEventHandler(_issuerMock.Object);
        var userProfileId = Guid.NewGuid();
        var emailId = Guid.NewGuid();
        var domainEvent = new EmailAddedDomainEvent(userProfileId, emailId, EmailAddress.Create("secondary@example.com"));

        await handler.Handle(domainEvent, CancellationToken.None);

        _issuerMock.Verify(x => x.IssueAsync(userProfileId, emailId, "secondary@example.com", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PassesCorrectEmailAddressToIssuer()
    {
        var handler = new EmailAddedDomainEventHandler(_issuerMock.Object);
        var userProfileId = Guid.NewGuid();
        var emailId = Guid.NewGuid();
        const string emailAddress = "test@flowchat.com";
        var domainEvent = new EmailAddedDomainEvent(userProfileId, emailId, EmailAddress.Create(emailAddress));

        await handler.Handle(domainEvent, CancellationToken.None);

        _issuerMock.Verify(x => x.IssueAsync(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            emailAddress,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
