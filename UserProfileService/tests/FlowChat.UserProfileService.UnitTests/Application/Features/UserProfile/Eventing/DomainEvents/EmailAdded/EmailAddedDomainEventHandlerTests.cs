using FlowChat.UserProfileService.Application.Features.UserProfile.Eventing.DomainEvents.EmailAdded;
using FlowChat.UserProfileService.Application.Features.UserProfile.EmailVerification.Interfaces;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationProcess;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;
using FlowChat.UserProfileService.Domain.Entities.UserProfile.Events;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class EmailAddedDomainEventHandlerTests
{
    private readonly Mock<IEmailVerificationProcessWriteRepository> _repositoryMock = new();
    private readonly Mock<IEmailVerificationRequestIssuer> _issuerMock = new();

    public EmailAddedDomainEventHandlerTests()
    {
        _repositoryMock
            .Setup(x => x.GetByEmailIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailVerificationProcess?)null);

        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<EmailVerificationProcess>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailVerificationProcess entity, CancellationToken _) => entity);

        _issuerMock
            .Setup(x => x.IssueAsync(
                It.IsAny<EmailVerificationProcess>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailVerificationProcess _, Guid userProfileId, Guid emailId, string _, CancellationToken _) =>
                EmailVerificationProcess
                    .Create(Id<UserProfile>.FromGuid(userProfileId), Id<Email>.FromGuid(emailId))
                    .IssueRequest(
                        Id<EmailVerificationRequest>.New(),
                        Guid.NewGuid().ToString("N"),
                        DateTimeOffset.UtcNow.AddHours(24),
                        DateTimeOffset.UtcNow));
    }

    [Fact]
    public async Task Handle_IssuesVerificationRequestForAddedEmail()
    {
        var handler = new EmailAddedDomainEventHandler(_repositoryMock.Object, _issuerMock.Object);
        var userProfileId = Guid.NewGuid();
        var emailId = Guid.NewGuid();
        var domainEvent = new EmailAddedDomainEvent(userProfileId, emailId, EmailAddress.Create("secondary@example.com"));

        await handler.Handle(domainEvent, CancellationToken.None);

        _repositoryMock.Verify(x => x.GetByEmailIdAsync(emailId, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(x => x.AddAsync(It.Is<EmailVerificationProcess>(process => process.Id.Value == emailId), It.IsAny<CancellationToken>()), Times.Once);
        _issuerMock.Verify(x => x.IssueAsync(
            It.Is<EmailVerificationProcess>(process => process.Id.Value == emailId),
            userProfileId,
            emailId,
            "secondary@example.com",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PassesCorrectEmailAddressToIssuer()
    {
        var handler = new EmailAddedDomainEventHandler(_repositoryMock.Object, _issuerMock.Object);
        var userProfileId = Guid.NewGuid();
        var emailId = Guid.NewGuid();
        const string emailAddress = "test@flowchat.com";
        var domainEvent = new EmailAddedDomainEvent(userProfileId, emailId, EmailAddress.Create(emailAddress));

        await handler.Handle(domainEvent, CancellationToken.None);

        _issuerMock.Verify(x => x.IssueAsync(
            It.IsAny<EmailVerificationProcess>(),
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            emailAddress,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithExistingProcess_PassesExistingProcessToIssuer()
    {
        var userProfileId = Id<UserProfile>.New();
        var emailId = Id<Email>.New();
        var process = EmailVerificationProcess.Create(userProfileId, emailId);
        var handler = new EmailAddedDomainEventHandler(_repositoryMock.Object, _issuerMock.Object);
        var domainEvent = new EmailAddedDomainEvent(userProfileId, emailId, EmailAddress.Create("secondary@example.com"));

        _repositoryMock
            .Setup(x => x.GetByEmailIdAsync(emailId.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(process);

        await handler.Handle(domainEvent, CancellationToken.None);

        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<EmailVerificationProcess>(), It.IsAny<CancellationToken>()), Times.Never);
        _issuerMock.Verify(x => x.IssueAsync(
            process,
            userProfileId.Value,
            emailId.Value,
            "secondary@example.com",
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
