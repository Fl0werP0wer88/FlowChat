using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.UserProfileService.Application.Contracts.Infrastructure;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Application.Features.UserProfile.EmailVerification;
using FlowChat.UserProfileService.Application.Features.UserProfile.EmailVerification.Interfaces;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationProcess;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class EmailVerificationRequestIssuerTests
{
    private readonly Mock<IEmailVerificationProcessWriteRepository> _repositoryMock = new();
    private readonly Mock<IEmailVerificationTokenProtector> _tokenProtectorMock = new();
    private readonly Mock<IEmailVerificationLinkBuilder> _linkBuilderMock = new();
    private readonly Mock<IOutboxIntegrationEventPublisher> _publisherMock = new();

    public EmailVerificationRequestIssuerTests()
    {
        _tokenProtectorMock
            .Setup(x => x.Protect(It.IsAny<EmailVerificationTokenPayload>()))
            .Returns("protected-token");

        _linkBuilderMock
            .Setup(x => x.BuildEmailVerificationLink(It.IsAny<string>()))
            .Returns<string>(token => $"https://frontend.flowchat.local/email-verification?token={token}");

        _publisherMock
            .Setup(x => x.PublishAsync(It.IsAny<IntegrationEventEnvelope<EmailVerificationRequestIntegrationEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(x => x.GetByEmailIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailVerificationProcess?)null);

        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<EmailVerificationProcess>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailVerificationProcess entity, CancellationToken _) => entity);

        _repositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<EmailVerificationProcess>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task IssueAsync_WhenProcessDoesNotExist_CreatesProcessAndPublishesIntegrationEvent()
    {
        var userProfileId = Guid.NewGuid();
        var emailId = Guid.NewGuid();

        EmailVerificationProcess? addedProcess = null;
        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<EmailVerificationProcess>(), It.IsAny<CancellationToken>()))
            .Callback<EmailVerificationProcess, CancellationToken>((entity, _) => addedProcess = entity)
            .ReturnsAsync((EmailVerificationProcess entity, CancellationToken _) => entity);

        EmailVerificationTokenPayload? capturedPayload = null;
        _tokenProtectorMock
            .Setup(x => x.Protect(It.IsAny<EmailVerificationTokenPayload>()))
            .Callback<EmailVerificationTokenPayload>(p => capturedPayload = p)
            .Returns("protected-token");

        IntegrationEventEnvelope<EmailVerificationRequestIntegrationEvent>? capturedEnvelope = null;
        _publisherMock
            .Setup(x => x.PublishAsync(It.IsAny<IntegrationEventEnvelope<EmailVerificationRequestIntegrationEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IntegrationEventEnvelope<EmailVerificationRequestIntegrationEvent>, CancellationToken>((envelope, _) => capturedEnvelope = envelope)
            .Returns(Task.CompletedTask);

        var sut = new EmailVerificationRequestIssuer(
            _repositoryMock.Object,
            _tokenProtectorMock.Object,
            _linkBuilderMock.Object,
            _publisherMock.Object);

        var result = await sut.IssueAsync(userProfileId, emailId, "john@example.com", CancellationToken.None);

        addedProcess.Should().NotBeNull();
        addedProcess!.Id.Value.Should().Be(emailId);
        addedProcess.Requests.Should().ContainSingle().Which.Should().BeSameAs(result);

        capturedPayload.Should().NotBeNull();
        capturedPayload!.UserProfileId.Should().Be(userProfileId);
        capturedPayload.EmailId.Should().Be(emailId);
        capturedPayload.Nonce.Should().Be(result.Nonce);

        capturedEnvelope.Should().NotBeNull();
        capturedEnvelope!.KafkaKey.Should().Be(result.Id.Value.ToString());
        var capturedEvent = capturedEnvelope.Payload;
        capturedEvent.VerificationRequestId.Should().Be(result.Id.Value);
        capturedEvent.UserId.Should().Be(userProfileId);
        capturedEvent.UserEmail.Should().Be("john@example.com");
        capturedEvent.ConfirmationLink.Should().Be("https://frontend.flowchat.local/email-verification?token=protected-token");
    }

    [Fact]
    public async Task IssueAsync_WhenProcessExists_InvalidatesExistingActiveRequestThroughAggregate()
    {
        var userProfileId = Id<UserProfile>.New();
        var emailId = Id<Email>.New();
        var process = EmailVerificationProcess.Create(userProfileId, emailId);
        var existingRequest = process.IssueRequest(
            Id<EmailVerificationRequest>.New(),
            "existing-nonce",
            DateTimeOffset.UtcNow.AddHours(6),
            DateTimeOffset.UtcNow);

        _repositoryMock
            .Setup(x => x.GetByEmailIdAsync(emailId.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(process);

        var sut = new EmailVerificationRequestIssuer(
            _repositoryMock.Object,
            _tokenProtectorMock.Object,
            _linkBuilderMock.Object,
            _publisherMock.Object);

        var result = await sut.IssueAsync(userProfileId.Value, emailId.Value, "john@example.com", CancellationToken.None);

        existingRequest.InvalidatedAtUtc.Should().NotBeNull();
        result.Should().NotBeSameAs(existingRequest);
        process.Requests.Count(x => x.IsActive(DateTimeOffset.UtcNow)).Should().Be(1);
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<EmailVerificationProcess>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
