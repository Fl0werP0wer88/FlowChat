using AutoMapper;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.UserProfileService.Application.Features.UserProfile.Eventing.DomainEvents.EmailConfirmed;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;
using FlowChat.UserProfileService.Domain.Entities.UserProfile.Events;
using Microsoft.Extensions.Logging.Abstractions;
using DomainEmail = FlowChat.UserProfileService.Domain.Entities.UserProfile.Email;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class EmailConfirmedDomainEventHandlerTests
{
    private readonly IMapper _mapper;
    private readonly Mock<IOutboxIntegrationEventPublisher> _publisherMock = new();

    public EmailConfirmedDomainEventHandlerTests()
    {
        _mapper = new MapperConfiguration(
                configuration => configuration.AddProfile<EmailConfirmedDomainEventToIntegrationEventProfile>(),
                NullLoggerFactory.Instance)
            .CreateMapper();

        _publisherMock
            .Setup(x => x.Publish(It.IsAny<IntegrationEventEnvelope<UserEmailConfirmedIntegrationEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Handle_PublishesMappedUserEmailConfirmedIntegrationEvent()
    {
        var handler = new EmailConfirmedDomainEventHandler(_publisherMock.Object, _mapper);
        var userProfileId = Id<UserProfile>.New();
        var emailId = Id<DomainEmail>.New();
        IntegrationEventEnvelope<UserEmailConfirmedIntegrationEvent>? capturedEnvelope = null;
        var domainEvent = new EmailConfirmedDomainEvent(
            userProfileId,
            emailId,
            EmailAddress.Create("john@example.com"),
            isAuth: true);

        _publisherMock
            .Setup(x => x.Publish(It.IsAny<IntegrationEventEnvelope<UserEmailConfirmedIntegrationEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IntegrationEventEnvelope<UserEmailConfirmedIntegrationEvent>, CancellationToken>((envelope, _) => capturedEnvelope = envelope)
            .Returns(Task.CompletedTask);

        await handler.Handle(domainEvent, CancellationToken.None);

        _publisherMock.Verify(x => x.Publish(
            It.IsAny<IntegrationEventEnvelope<UserEmailConfirmedIntegrationEvent>>(),
            It.IsAny<CancellationToken>()), Times.Once);

        capturedEnvelope.Should().NotBeNull();
        var integrationEvent = capturedEnvelope!.Payload;
        integrationEvent.UserProfileId.Should().Be(userProfileId.Value);
        integrationEvent.EmailId.Should().Be(emailId.Value);
        integrationEvent.Email.Address.Should().Be("john@example.com");
        integrationEvent.Email.IsAuth.Should().BeTrue();
        capturedEnvelope.KafkaKey.Should().Be(userProfileId.Value.ToString());
    }

    [Fact]
    public async Task Handle_WithNonAuthEmail_MapsIsAuthFalse()
    {
        var handler = new EmailConfirmedDomainEventHandler(_publisherMock.Object, _mapper);
        var userProfileId = Id<UserProfile>.New();
        var emailId = Id<DomainEmail>.New();
        var domainEvent = new EmailConfirmedDomainEvent(
            userProfileId,
            emailId,
            EmailAddress.Create("secondary@example.com"),
            isAuth: false);

        IntegrationEventEnvelope<UserEmailConfirmedIntegrationEvent>? capturedEnvelope = null;
        _publisherMock
            .Setup(x => x.Publish(It.IsAny<IntegrationEventEnvelope<UserEmailConfirmedIntegrationEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IntegrationEventEnvelope<UserEmailConfirmedIntegrationEvent>, CancellationToken>((envelope, _) => capturedEnvelope = envelope)
            .Returns(Task.CompletedTask);

        await handler.Handle(domainEvent, CancellationToken.None);

        capturedEnvelope.Should().NotBeNull();
        var integrationEvent = capturedEnvelope!.Payload;
        integrationEvent.Email.IsAuth.Should().BeFalse();
        integrationEvent.Email.Address.Should().Be("secondary@example.com");
    }
}
