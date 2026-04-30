using AutoMapper;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.UserProfileService.Application.Features.UserProfile.EmailVerification.Interfaces;
using FlowChat.UserProfileService.Application.Features.UserProfile.Eventing.DomainEvents.UserProfileCreated;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;
using FlowChat.UserProfileService.Domain.Entities.UserProfile.Events;
using Microsoft.Extensions.Logging.Abstractions;
using DomainEmail = FlowChat.UserProfileService.Domain.Entities.UserProfile.Email;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class UserProfileCreatedDomainEventHandlerTests
{
    private readonly IMapper _mapper;
    private readonly Mock<IOutboxIntegrationEventPublisher> _publisherMock = new();
    private readonly Mock<IEmailVerificationRequestIssuer> _issuerMock = new();

    public UserProfileCreatedDomainEventHandlerTests()
    {
        _mapper = new MapperConfiguration(
                configuration => configuration.AddProfile<UserProfileCreatedDomainEventToIntegrationEventProfile>(),
                NullLoggerFactory.Instance)
            .CreateMapper();

        _publisherMock
            .Setup(x => x.Publish(It.IsAny<IntegrationEventEnvelope<UserProfileCreatedIntegrationEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _issuerMock
            .Setup(x => x.IssueAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid userProfileId, Guid emailId, string _, CancellationToken _) =>
                EmailVerificationRequest.Create(Id<EmailVerificationRequest>.New(), userProfileId, emailId, Guid.NewGuid().ToString("N"), DateTimeOffset.UtcNow.AddHours(24)));
    }

    [Fact]
    public async Task Handle_PublishesMappedUserProfileCreatedIntegrationEvent()
    {
        var handler = new UserProfileCreatedDomainEventHandler(_publisherMock.Object, _mapper, _issuerMock.Object);
        var userProfileId = Id<UserProfile>.New();
        var mainEmailId = Id<DomainEmail>.New();
        var domainEvent = new UserProfileCreatedDomainEvent(
            userProfileId,
            mainEmailId,
            Id<Phone>.New(),
            "jdoe",
            EmailAddress.Create("john@example.com"),
            PhoneNumber.Create("+48123123123"),
            "https://cdn.example/avatar.png",
            "about me",
            true,
            new DateTimeOffset(2026, 3, 10, 9, 0, 0, TimeSpan.Zero),
            "John",
            "Doe",
            "FlowChat");

        IntegrationEventEnvelope<UserProfileCreatedIntegrationEvent>? capturedEnvelope = null;
        _publisherMock
            .Setup(x => x.Publish(It.IsAny<IntegrationEventEnvelope<UserProfileCreatedIntegrationEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IntegrationEventEnvelope<UserProfileCreatedIntegrationEvent>, CancellationToken>((envelope, _) => capturedEnvelope = envelope)
            .Returns(Task.CompletedTask);

        await handler.Handle(domainEvent, CancellationToken.None);

        capturedEnvelope.Should().NotBeNull();
        capturedEnvelope!.KafkaKey.Should().Be(userProfileId.Value.ToString());
        var capturedEvent = capturedEnvelope.Payload;
        capturedEvent.UserProfileId.Should().Be(userProfileId.Value);
        capturedEvent.FriendlyUserId.Should().Be("jdoe");
        capturedEvent.MainEmail.Address.Should().Be("john@example.com");
        capturedEvent.MainEmail.IsConfirmed.Should().BeFalse();
        capturedEvent.MainEmail.IsVisible.Should().BeTrue();
        capturedEvent.MainPhone.Should().NotBeNull();
        capturedEvent.MainPhone!.Number.Should().Be("+48123123123");
        capturedEvent.MainPhone.IsConfirmed.Should().BeFalse();
        capturedEvent.MainPhone.IsVisible.Should().BeTrue();
        capturedEvent.AvatarUrl.Should().Be("https://cdn.example/avatar.png");
        capturedEvent.Bio.Should().Be("about me");
        capturedEvent.IsActive.Should().BeTrue();
        capturedEvent.LastSeenAtUtc.Should().Be(new DateTimeOffset(2026, 3, 10, 9, 0, 0, TimeSpan.Zero));
        capturedEvent.FirstName.Should().Be("John");
        capturedEvent.LastName.Should().Be("Doe");
        capturedEvent.Organization.Should().Be("FlowChat");

        _issuerMock.Verify(x => x.IssueAsync(userProfileId.Value, mainEmailId.Value, "john@example.com", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithoutPhone_MapsNullMainPhone()
    {
        var handler = new UserProfileCreatedDomainEventHandler(_publisherMock.Object, _mapper, _issuerMock.Object);
        var userProfileId = Id<UserProfile>.New();
        var mainEmailId = Id<DomainEmail>.New();
        var domainEvent = new UserProfileCreatedDomainEvent(
            userProfileId,
            mainEmailId,
            null,
            "jdoe",
            EmailAddress.Create("john@example.com"),
            null,
            null,
            null,
            true,
            null);

        IntegrationEventEnvelope<UserProfileCreatedIntegrationEvent>? capturedEnvelope = null;
        _publisherMock
            .Setup(x => x.Publish(It.IsAny<IntegrationEventEnvelope<UserProfileCreatedIntegrationEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IntegrationEventEnvelope<UserProfileCreatedIntegrationEvent>, CancellationToken>((envelope, _) => capturedEnvelope = envelope)
            .Returns(Task.CompletedTask);

        await handler.Handle(domainEvent, CancellationToken.None);

        capturedEnvelope.Should().NotBeNull();
        var capturedEvent = capturedEnvelope!.Payload;
        capturedEvent.MainEmail.Should().NotBeNull();
        capturedEvent.MainEmail.Address.Should().Be("john@example.com");
        capturedEvent!.MainPhone.Should().BeNull();
    }
}
