using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.Shared.Application;
using FlowChat.UserProfileService.Application.Contracts.Infrastructure;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Application.Features.UserProfile.EmailVerification;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;
using FlowChat.UserProfileService.Infrastructure.Services;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class EmailVerificationRequestIssuerTests
{
    private readonly Mock<IEmailVerificationRequestWriteRepository> _repositoryMock = new();
    private readonly Mock<IEmailVerificationTokenProtector> _tokenProtectorMock = new();
    private readonly Mock<IEmailVerificationLinkBuilder> _linkBuilderMock = new();
    private readonly Mock<IIntegrationEventPublisher> _publisherMock = new();

    public EmailVerificationRequestIssuerTests()
    {
        _tokenProtectorMock
            .Setup(x => x.Protect(It.IsAny<EmailVerificationTokenPayload>()))
            .Returns("protected-token");

        _linkBuilderMock
            .Setup(x => x.BuildEmailVerificationLink(It.IsAny<string>()))
            .Returns<string>(token => $"https://frontend.flowchat.local/email-verification?token={token}");

        _publisherMock
            .Setup(x => x.Publish(It.IsAny<EmailVerificationRequestIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(x => x.GetActiveByEmailIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<EmailVerificationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailVerificationRequest entity, CancellationToken _) => entity);

        _repositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<EmailVerificationRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task IssueAsync_InvalidatesExistingRequestsAndPublishesIntegrationEvent()
    {
        var userProfileId = Guid.NewGuid();
        var emailId = Guid.NewGuid();
        var existingRequest = EmailVerificationRequest.Create(
            userProfileId,
            emailId,
            "existing-nonce",
            DateTimeOffset.UtcNow.AddHours(6));

        _repositoryMock
            .Setup(x => x.GetActiveByEmailIdAsync(emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([existingRequest]);

        EmailVerificationRequest? addedEntity = null;
        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<EmailVerificationRequest>(), It.IsAny<CancellationToken>()))
            .Callback<EmailVerificationRequest, CancellationToken>((entity, _) => addedEntity = entity)
            .ReturnsAsync((EmailVerificationRequest entity, CancellationToken _) => entity);

        EmailVerificationTokenPayload? capturedPayload = null;
        _tokenProtectorMock
            .Setup(x => x.Protect(It.IsAny<EmailVerificationTokenPayload>()))
            .Callback<EmailVerificationTokenPayload>(p => capturedPayload = p)
            .Returns("protected-token");

        EmailVerificationRequestIntegrationEvent? capturedEvent = null;
        _publisherMock
            .Setup(x => x.Publish(It.IsAny<EmailVerificationRequestIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Callback<EmailVerificationRequestIntegrationEvent, CancellationToken>((evt, _) => capturedEvent = evt)
            .Returns(Task.CompletedTask);

        var sut = new EmailVerificationRequestIssuer(
            _repositoryMock.Object,
            _tokenProtectorMock.Object,
            _linkBuilderMock.Object,
            _publisherMock.Object);

        var result = await sut.IssueAsync(userProfileId, emailId, "john@example.com", CancellationToken.None);

        existingRequest.InvalidatedAtUtc.Should().NotBeNull();
        addedEntity.Should().NotBeNull();
        addedEntity.Should().BeSameAs(result);

        capturedPayload.Should().NotBeNull();
        capturedPayload!.UserProfileId.Should().Be(userProfileId);
        capturedPayload.EmailId.Should().Be(emailId);
        capturedPayload.Nonce.Should().Be(result.Nonce);

        capturedEvent.Should().NotBeNull();
        capturedEvent!.Key.Should().Be(result.Id.Value.ToString());
        capturedEvent.UserId.Should().Be(userProfileId);
        capturedEvent.UserEmail.Should().Be("john@example.com");
        capturedEvent.ConfirmationLink.Should().Be("https://frontend.flowchat.local/email-verification?token=protected-token");
    }
}
