using AutoMapper;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Application.Features.UserProfile.Eventing.DomainEvents.AuthEmailChanged;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;
using FlowChat.UserProfileService.Domain.Entities.UserProfile.Events;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using DomainEmail = FlowChat.UserProfileService.Domain.Entities.UserProfile.Email;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class AuthEmailChangedDomainEventHandlerTests
{
    private readonly IMapper _mapper;
    private readonly Mock<IOutboxIntegrationEventPublisher> _publisherMock = new();

    public AuthEmailChangedDomainEventHandlerTests()
    {
        _mapper = new MapperConfiguration(
                configuration => configuration.AddProfile<AuthEmailChangedDomainEventToIntegrationEventProfile>(),
                NullLoggerFactory.Instance)
            .CreateMapper();

        _publisherMock
            .Setup(x => x.PublishAsync(It.IsAny<IntegrationEventEnvelope<AuthEmailChangedIntegrationEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Handle_PublishesMappedAuthEmailChangedIntegrationEvent()
    {
        var handler = new AuthEmailChangedDomainEventHandler(_publisherMock.Object, _mapper);
        var userProfileId = Id<UserProfile>.New();
        var emailId = Id<DomainEmail>.New();
        IntegrationEventEnvelope<AuthEmailChangedIntegrationEvent>? capturedEnvelope = null;
        var domainEvent = new AuthEmailChangedDomainEvent(
            userProfileId,
            emailId,
            EmailAddress.Create("john@example.com"));

        _publisherMock
            .Setup(x => x.PublishAsync(It.IsAny<IntegrationEventEnvelope<AuthEmailChangedIntegrationEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IntegrationEventEnvelope<AuthEmailChangedIntegrationEvent>, CancellationToken>((envelope, _) => capturedEnvelope = envelope)
            .Returns(Task.CompletedTask);

        await handler.Handle(domainEvent, CancellationToken.None);

        _publisherMock.Verify(x => x.PublishAsync(
            It.IsAny<IntegrationEventEnvelope<AuthEmailChangedIntegrationEvent>>(),
            It.IsAny<CancellationToken>()), Times.Once);

        capturedEnvelope.Should().NotBeNull();
        var integrationEvent = capturedEnvelope!.Payload;
        integrationEvent.UserProfileId.Should().Be(userProfileId.Value);
        integrationEvent.EmailId.Should().Be(emailId.Value);
        integrationEvent.EmailAddress.Should().Be("john@example.com");
        capturedEnvelope.KafkaKey.Should().Be(userProfileId.Value.ToString());
    }
}
